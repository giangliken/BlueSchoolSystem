using BlueSchoolSystem.Models;
using BlueSchoolSystem.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlueSchoolSystem.Services
{
    public class LichThiService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LichThiService> _logger;

        public LichThiService(ApplicationDbContext context, ILogger<LichThiService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> AutoScheduleExamAsync(int lopHocPhanId)
        {
            try
            {
                // 1. Lấy thông tin Lớp học phần (Giữ nguyên)
                var lhp = await _context.LopHocPhans
                    .Include(l => l.MonHoc)
                    .Include(l => l.HocKy)
                    .FirstOrDefaultAsync(l => l.Id == lopHocPhanId);

                if (lhp == null) return (false, "Không tìm thấy lớp học phần.");

                // 2. Kiểm tra đã có lịch chưa (Giữ nguyên)
                bool hasExam = await _context.LichThis.AnyAsync(lt => lt.LopHocPhanId == lopHocPhanId);
                if (hasExam) return (false, "Lớp này đã có lịch thi rồi.");

                // ========================================================================
                // [QUAN TRỌNG] LẤY ID TRẠNG THÁI ĐỘNG (KHÔNG GÁN CỨNG SỐ 33)
                // ========================================================================
                var trangThaiChuaDienRa = await _context.TrangThais
                    .FirstOrDefaultAsync(t => t.LoaiTrangThai == "LichThi" && t.TenTrangThai == "Chưa diễn ra");

                if (trangThaiChuaDienRa == null)
                {
                    // Nếu chưa có thì báo lỗi hệ thống (hoặc có thể code tự tạo mới luôn tại đây nếu muốn)
                    return (false, "Lỗi hệ thống: Không tìm thấy trạng thái 'Chưa diễn ra' trong CSDL. Vui lòng kiểm tra bảng TrangThai.");
                }

                int targetTrangThaiId = trangThaiChuaDienRa.Id;
                // ========================================================================

                // 3. Phân tích loại phòng (Giữ nguyên)
                var (hinhThucThi, loaiPhongCanTim) = AnalyzeExamType(lhp.MonHoc.MoTa);

                // 4. Lấy danh sách phòng thi (Giữ nguyên)
                var phongThiList = await _context.PhongHocs.ToListAsync();
                var phongPhuHop = phongThiList.Where(p =>
                {
                    bool isPhongThucHanh = p.MaPhongHoc.ToUpper().Contains("TH") ||
                                           p.TenPhongHoc.ToUpper().Contains("THỰC HÀNH") ||
                                           p.MaPhongHoc.ToUpper().Contains("PM");

                    return loaiPhongCanTim == "TH" ? isPhongThucHanh : !isPhongThucHanh;
                }).ToList();

                if (!phongPhuHop.Any()) return (false, $"Không tìm thấy phòng thi phù hợp ({hinhThucThi}).");

                // 5. Xác định thời gian (Giữ nguyên)
                DateTime searchEndDate = lhp.NgayKetThuc;
                DateTime searchStartDate = lhp.NgayKetThuc.AddDays(-14);
                if (searchStartDate < lhp.NgayBatDau) searchStartDate = lhp.NgayBatDau;

                // 6. Thuật toán tìm slot trống
                for (DateTime date = searchStartDate; date <= searchEndDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                    var caThiList = GetCaThiList(date);

                    foreach (var ca in caThiList)
                    {
                        foreach (var phong in phongPhuHop)
                        {
                            // Kiểm tra trùng (Giữ nguyên)
                            bool isRoomBusy = await _context.LichThis.AnyAsync(lt =>
                                lt.PhongHocId == phong.Id &&
                                lt.NgayThi.Date == date.Date &&
                                ((lt.GioBatDau < ca.KetThuc) && (lt.GioKetThuc > ca.BatDau))
                            );

                            bool isClassBusy = await _context.LichHocs.AnyAsync(lh =>
                                lh.LopHocPhanId == lhp.Id &&
                                lh.Ngay.Date == date.Date &&
                                ((lh.GioBatDau < ca.KetThuc) && (lh.GioKetThuc > ca.BatDau))
                            );

                            if (!isRoomBusy && !isClassBusy)
                            {
                                // === TÌM THẤY SLOT TRỐNG ===
                                var lichThiMoi = new LichThi
                                {
                                    LopHocPhanId = lhp.Id,
                                    NgayThi = date,
                                    GioBatDau = ca.BatDau,
                                    GioKetThuc = ca.KetThuc,
                                    HinhThucThi = hinhThucThi,
                                    PhongHocId = phong.Id,

                                    // SỬ DỤNG BIẾN ĐÃ LẤY TỪ DB
                                    TrangThaiId = targetTrangThaiId
                                };

                                _context.LichThis.Add(lichThiMoi);
                                await _context.SaveChangesAsync();

                                return (true, $"Đã xếp lịch: {date:dd/MM/yyyy} ({ca.BatDau:hh\\:mm}-{ca.KetThuc:hh\\:mm}) tại {phong.MaPhongHoc}");
                            }
                        }
                    }
                }

                return (false, "Không tìm được phòng trống trong 2 tuần cuối môn học.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xếp lịch thi tự động.");
                return (false, "Lỗi hệ thống: " + ex.Message);
            }
        }

        // --- CÁC HÀM HELPER ---

        private (string HinhThuc, string LoaiPhong) AnalyzeExamType(string? moTa)
        {
            if (string.IsNullOrEmpty(moTa)) return ("Tự luận", "LT");

            string desc = moTa.ToUpper();
            if (desc.Contains("TNM")) return ("Trắc nghiệm máy", "TH");
            if (desc.Contains("TNG")) return ("Trắc nghiệm giấy", "LT");
            if (desc.Contains("BC") || desc.Contains("BÁO CÁO")) return ("Báo cáo", "LT");
            if (desc.Contains("TL") || desc.Contains("TỰ LUẬN")) return ("Tự luận", "LT");

            return ("Tự luận", "LT");
        }

        private List<(TimeSpan BatDau, TimeSpan KetThuc)> GetCaThiList(DateTime date)
        {
            return new List<(TimeSpan, TimeSpan)>
            {
                (new TimeSpan(7, 30, 0), new TimeSpan(9, 0, 0)),   // Ca 1
                (new TimeSpan(9, 30, 0), new TimeSpan(11, 0, 0)),  // Ca 2
                (new TimeSpan(13, 30, 0), new TimeSpan(15, 0, 0)), // Ca 3
                (new TimeSpan(15, 30, 0), new TimeSpan(17, 0, 0))  // Ca 4
            };
        }
    }
}