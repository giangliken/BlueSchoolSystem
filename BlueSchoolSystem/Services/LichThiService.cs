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

        // [UPDATE] Thêm tham số optional relatedClassIds
        // Thêm tham số optional List<int> relatedClassIds vào định nghĩa hàm
        public async Task<(bool Success, string Message)> AutoScheduleExamAsync(int lopHocPhanId, List<int> relatedClassIds = null)
        {
            try
            {
                // 1. Lấy thông tin Lớp học phần
                var lhp = await _context.LopHocPhans
                    .Include(l => l.MonHoc)
                    .Include(l => l.HocKy)
                    .FirstOrDefaultAsync(l => l.Id == lopHocPhanId);

                if (lhp == null) return (false, "Không tìm thấy lớp học phần.");

                // 2. Kiểm tra đã có lịch chưa
                bool hasExam = await _context.LichThis.AnyAsync(lt => lt.LopHocPhanId == lopHocPhanId);
                if (hasExam) return (false, "Lớp này đã có lịch thi rồi.");

                // 3. Lấy trạng thái
                var trangThaiChuaDienRa = await _context.TrangThais.FirstOrDefaultAsync(t => t.LoaiTrangThai == "LichThi" && t.TenTrangThai == "Chưa diễn ra");
                int targetTrangThaiId = trangThaiChuaDienRa?.Id ?? 33;

                // 4. Phân tích loại phòng
                var (hinhThucThi, loaiPhongCanTim) = AnalyzeExamType(lhp.MonHoc.MoTa);

                // Lấy danh sách phòng phù hợp
                var phongThiList = await _context.PhongHocs.ToListAsync();
                var phongPhuHop = phongThiList.Where(p =>
                {
                    bool isPhongThucHanh = p.MaPhongHoc.ToUpper().Contains("TH") || p.TenPhongHoc.ToUpper().Contains("THỰC HÀNH") || p.MaPhongHoc.ToUpper().Contains("PM");
                    return loaiPhongCanTim == "TH" ? isPhongThucHanh : !isPhongThucHanh;
                }).ToList();

                if (!phongPhuHop.Any()) return (false, $"Không tìm thấy phòng thi phù hợp ({hinhThucThi}).");

                // 5. Xác định thời gian
                DateTime searchEndDate = lhp.NgayKetThuc;
                DateTime searchStartDate = lhp.NgayKetThuc.AddDays(-14);
                if (searchStartDate < lhp.NgayBatDau) searchStartDate = lhp.NgayBatDau;

                // ========================================================================
                // [LOGIC MỚI - ĐÃ FIX] CHECK TRÙNG DỰA TRÊN DANH SÁCH ID LỚP (List<int>)
                // ========================================================================

                // Nếu danh sách lớp chưa được truyền vào (null hoặc rỗng), tự tìm trong DB
                if (relatedClassIds == null || !relatedClassIds.Any())
                {
                    relatedClassIds = await _context.ChiTietLopHocPhans
                        .Where(ct => ct.LopHocPhanId == lopHocPhanId)
                        .Select(ct => ct.SinhVien.LopId)
                        .Where(id => id.HasValue) // Lọc null
                        .Select(id => id.Value)   // Lấy int
                        .Distinct()
                        .ToListAsync();
                }

                var busyClassSchedules = new List<(DateTime Ngay, TimeSpan Start, TimeSpan End)>();

                if (relatedClassIds.Any())
                {
                    // Tìm các LHP khác mà sinh viên của các lớp này đang học
                    var lhpIdsOfSameClass = await _context.ChiTietLopHocPhans
                        .Include(ct => ct.LopHocPhan)
                        // So sánh int với List<int> (An toàn vì đã xử lý Value ở trên)
                        .Where(ct => ct.SinhVien.LopId.HasValue && relatedClassIds.Contains(ct.SinhVien.LopId.Value))
                        .Where(ct => ct.LopHocPhan.HocKyId == lhp.HocKyId)
                        .Where(ct => ct.LopHocPhanId != lopHocPhanId)
                        .Select(ct => ct.LopHocPhanId)
                        .Distinct()
                        .ToListAsync();

                    // Lấy lịch thi của các môn đó
                    var rawSchedules = await _context.LichThis
                        .Where(lt => lhpIdsOfSameClass.Contains(lt.LopHocPhanId))
                        .Select(lt => new { lt.NgayThi, lt.GioBatDau, lt.GioKetThuc })
                        .ToListAsync();

                    busyClassSchedules = rawSchedules.Select(x => (x.NgayThi.Date, x.GioBatDau, x.GioKetThuc)).ToList();
                }

                // 6. THUẬT TOÁN TÌM SLOT
                for (DateTime date = searchStartDate; date <= searchEndDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek == DayOfWeek.Sunday) continue;

                    var caThiList = GetCaThiList(date);

                    foreach (var ca in caThiList)
                    {
                        // [CHECK 1] Kiểm tra LỊCH SINH VIÊN (Lớp hành chính)
                        bool isStudentBusy = busyClassSchedules.Any(busy =>
                            busy.Ngay == date.Date && // Cùng ngày
                            ((busy.Start < ca.KetThuc) && (busy.End > ca.BatDau)) // Giao nhau về giờ
                        );

                        if (isStudentBusy) continue; // Nếu bận thì bỏ qua ca này ngay

                        bool isClassAlreadyHasExamThatDay = busyClassSchedules.Any(busy =>
                            busy.Ngay == date.Date
                        );

                        if (isClassAlreadyHasExamThatDay) continue;
                        foreach (var phong in phongPhuHop)
                        {
                            // [CHECK 2] Kiểm tra TRÙNG PHÒNG
                            bool isRoomBusy = await _context.LichThis.AnyAsync(lt =>
                                lt.PhongHocId == phong.Id &&
                                lt.NgayThi.Date == date.Date &&
                                ((lt.GioBatDau < ca.KetThuc) && (lt.GioKetThuc > ca.BatDau))
                            );

                            // [CHECK 3] Kiểm tra TRÙNG LỊCH HỌC
                            bool isClassStudyBusy = await _context.LichHocs.AnyAsync(lh =>
                                lh.LopHocPhanId == lhp.Id &&
                                lh.Ngay.Date == date.Date &&
                                ((lh.GioBatDau < ca.KetThuc) && (lh.GioKetThuc > ca.BatDau))
                            );

                            if (!isRoomBusy && !isClassStudyBusy)
                            {
                                var lichThiMoi = new LichThi
                                {
                                    LopHocPhanId = lhp.Id,
                                    NgayThi = date,
                                    GioBatDau = ca.BatDau,
                                    GioKetThuc = ca.KetThuc,
                                    HinhThucThi = hinhThucThi,
                                    PhongHocId = phong.Id,
                                    TrangThaiId = targetTrangThaiId
                                };

                                _context.LichThis.Add(lichThiMoi);
                                await _context.SaveChangesAsync();

                                return (true, $"Đã xếp: {date:dd/MM} ({ca.BatDau:hh\\:mm}-{ca.KetThuc:hh\\:mm}) tại {phong.MaPhongHoc}");
                            }
                        }
                    }
                }

                return (false, "Không tìm được lịch trống.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xếp lịch thi.");
                return (false, "Lỗi hệ thống: " + ex.Message);
            }
        }

        // ... Helpers giữ nguyên ...
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
                (new TimeSpan(7, 30, 0), new TimeSpan(9, 0, 0)),
                (new TimeSpan(9, 30, 0), new TimeSpan(11, 0, 0)),
                (new TimeSpan(13, 30, 0), new TimeSpan(15, 0, 0)),
                (new TimeSpan(15, 30, 0), new TimeSpan(17, 0, 0))
            };
        }
    }
}