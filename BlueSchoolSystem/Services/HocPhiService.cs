using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlueSchoolSystem.Services
{
    public class HocPhiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public HocPhiService(ApplicationDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // --- HELPER: Lấy ID trạng thái ---
        private async Task<(int ChuaDong, int MotPhan, int HoanThanh)> GetHocPhiStatusIds()
        {
            var statuses = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "HocPhi")
                .ToListAsync();

            int chuaDong = statuses.FirstOrDefault(t => t.TenTrangThai == "Chưa đóng")?.Id ?? 0;
            int motPhan = statuses.FirstOrDefault(t => t.TenTrangThai == "Đóng một phần")?.Id ?? 0;
            int hoanThanh = statuses.FirstOrDefault(t => t.TenTrangThai == "Đã hoàn thành")?.Id ?? 0;

            if (chuaDong == 0) return (1, 2, 3);
            return (chuaDong, motPhan, hoanThanh);
        }

        // --- HELPER: Lấy hoặc tạo ví ---
        private async Task<TaiKhoanSinhVien> GetOrCreateTaiKhoanAsync(int sinhVienId)
        {
            var tk = await _context.TaiKhoanSinhViens
                .Include(t => t.SinhVien) // Include để lấy MSSV cho log
                .FirstOrDefaultAsync(t => t.SinhVienId == sinhVienId);

            if (tk == null)
            {
                // Lấy trạng thái mặc định (ví dụ ID 1 = Hoạt động)
                int trangThaiActive = 1;

                tk = new TaiKhoanSinhVien
                {
                    SinhVienId = sinhVienId,
                    SoDu = 0,
                    TrangThaiId = trangThaiActive,
                    NgayCapNhatCuoi = DateTime.Now
                };
                _context.TaiKhoanSinhViens.Add(tk);
                await _context.SaveChangesAsync();

                // Load lại thông tin SinhVien để tránh NullReference khi ghi log
                await _context.Entry(tk).Reference(t => t.SinhVien).LoadAsync();
            }
            return tk;
        }

        // 1. LẤY DANH SÁCH CÔNG NỢ
        public async Task<List<HocPhiDashboardVM>> GetDanhSachCongNoAsync(string? keyword, int? lopId)
        {
            var (_, _, sttHoanThanh) = await GetHocPhiStatusIds();

            var query = _context.SinhViens
                .Include(sv => sv.Lop)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(sv => sv.MSSV.Contains(keyword) || (sv.HoVaTenDem + " " + sv.Ten).Contains(keyword));
            }
            if (lopId.HasValue)
            {
                query = query.Where(sv => sv.LopId == lopId);
            }

            var result = await query.Select(sv => new HocPhiDashboardVM
            {
                SinhVienId = sv.Id,
                MSSV = sv.MSSV,
                HoTen = sv.HoVaTenDem + " " + sv.Ten,
                TenLop = sv.Lop != null ? sv.Lop.MaLop : "Chưa phân lớp",

                TongNo = _context.HoaDonHocPhis
                            .Where(hd => hd.SinhVienId == sv.Id && hd.TrangThaiId != sttHoanThanh)
                            .Sum(hd => hd.ConLai),

                SoDuVi = _context.TaiKhoanSinhViens
                            .Where(t => t.SinhVienId == sv.Id)
                            .Select(t => t.SoDu)
                            .FirstOrDefault()
            }).ToListAsync();

            return result;
        }

        // 2. CẬP NHẬT HỌC PHÍ (TÍNH TOÁN LẠI)
        public async Task CalculateTuitionFeeAsync(int sinhVienId, int hocKyId, string adminUser)
        {
            var (sttChuaDong, sttMotPhan, sttHoanThanh) = await GetHocPhiStatusIds();

            var sv = await _context.SinhViens.Include(s => s.Lop).FirstOrDefaultAsync(s => s.Id == sinhVienId);
            var hk = await _context.HocKys.FindAsync(hocKyId);

            if (sv == null || hk == null) throw new Exception("Dữ liệu không hợp lệ");

            int startYear = hk.NgayBatDau.Year;
            if (hk.NgayBatDau.Month < 6) startYear--;
            string namHocStr = $"{startYear}-{startYear + 1}";

            var dinhMuc = await _context.DinhMucHocPhis
                .Where(d => d.NamHoc == namHocStr)
                .Where(d => d.NganhId == sv.Lop.NganhId)
                .FirstOrDefaultAsync();

            if (dinhMuc == null)
            {
                dinhMuc = await _context.DinhMucHocPhis
                    .Where(d => d.NamHoc == namHocStr && d.NganhId == null)
                    .FirstOrDefaultAsync();

                if (dinhMuc == null) throw new Exception($"Chưa cấu hình học phí cho ngành {sv.Lop?.NganhId} năm {namHocStr}.");
            }

            decimal giaTinChi = dinhMuc.GiaTienMotTinChi;

            var dsDangKy = await _context.DangKyHocPhans
                .Include(dk => dk.LopHocPhan).ThenInclude(l => l.MonHoc)
                .Where(dk => dk.SinhVienId == sinhVienId && dk.LopHocPhan.HocKyId == hocKyId)
                .ToListAsync();

            int hocKyNumber = GetHocKyNumber(hk.TenHocKy);
            var hoaDon = await _context.HoaDonHocPhis
                .Include(hd => hd.ChiTietHoaDons)
                .FirstOrDefaultAsync(hd => hd.SinhVienId == sinhVienId && hd.HocKy == hocKyNumber && hd.NamHoc == namHocStr);

            if (hoaDon == null)
            {
                hoaDon = new HoaDonHocPhi
                {
                    SinhVienId = sinhVienId,
                    HocKy = hocKyNumber,
                    NamHoc = namHocStr,
                    TrangThaiId = sttChuaDong,
                    TongTien = 0,
                    DaDong = 0,
                    ConLai = 0,
                    ChiTietHoaDons = new List<ChiTietHoaDon>()
                };
                _context.HoaDonHocPhis.Add(hoaDon);
            }

            decimal tongTienMoi = 0;
            if (hoaDon.ChiTietHoaDons != null) _context.ChiTietHoaDons.RemoveRange(hoaDon.ChiTietHoaDons);

            foreach (var item in dsDangKy)
            {
                var mon = item.LopHocPhan.MonHoc;
                decimal thanhTien = mon.SoTinChi * giaTinChi;
                tongTienMoi += thanhTien;

                var chiTiet = new ChiTietHoaDon
                {
                    HoaDonHocPhi = hoaDon,
                    DangKyHocPhanId = item.Id,
                    SoTien = thanhTien
                };
                _context.ChiTietHoaDons.Add(chiTiet);
            }

            hoaDon.TongTien = tongTienMoi;
            hoaDon.ConLai = hoaDon.TongTien - hoaDon.DaDong;

            if (hoaDon.ConLai <= 0 && hoaDon.TongTien > 0) hoaDon.TrangThaiId = sttHoanThanh;
            else if (hoaDon.DaDong > 0) hoaDon.TrangThaiId = sttMotPhan;
            else hoaDon.TrangThaiId = sttChuaDong;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(null, adminUser, "System", "::1", "UPDATE_HOCPHI", "HoaDonHocPhi", hoaDon.Id.ToString(), $"Cập nhật học phí SV {sv.MSSV}. Tổng: {tongTienMoi:N0}");
        }

        // 3. CHỨC NĂNG MỚI: NẠP TIỀN VÀO VÍ
        public async Task NapTienVaoViAsync(int sinhVienId, decimal soTien, string adminUser, string ghiChu)
        {
            if (soTien <= 0) throw new Exception("Số tiền nạp phải lớn hơn 0");

            var tk = await GetOrCreateTaiKhoanAsync(sinhVienId);

            var gd = new GiaoDichThanhToan
            {
                TaiKhoanSinhVienId = tk.Id,
                HoaDonHocPhiId = null,
                SoTien = soTien,
                LoaiGiaoDich = 1, // 1 = Nạp tiền
                HinhThuc = "TienMat",
                NguoiThucHien = adminUser,
                NgayThanhToan = DateTime.Now,
                GhiChu = string.IsNullOrEmpty(ghiChu) ? "Nạp tiền vào ví" : ghiChu
            };
            _context.GiaoDichThanhToans.Add(gd);

            tk.SoDu += soTien;
            tk.NgayCapNhatCuoi = DateTime.Now;

            await _context.SaveChangesAsync();

            string mssv = tk.SinhVien?.MSSV ?? "Unknown";
            await _activityLogService.LogAsync(null, adminUser, "System", "::1", "DEPOSIT", "TaiKhoanSinhVien", tk.Id.ToString(), $"Nạp {soTien:N0}đ vào ví SV {mssv}");
        }

        // 4. CHỨC NĂNG MỚI: THANH TOÁN HÓA ĐƠN
        internal async Task ThanhToanHocPhiAsync(int hoaDonId, decimal soTienMuonDong, string adminUser)
        {
            var (sttChuaDong, sttMotPhan, sttHoanThanh) = await GetHocPhiStatusIds();

            // Tìm hóa đơn kèm thông tin SV để log (nếu cần)
            var hoaDon = await _context.HoaDonHocPhis
                .Include(h => h.SinhVien)
                .FirstOrDefaultAsync(h => h.Id == hoaDonId);

            if (hoaDon == null) throw new Exception("Hóa đơn không tồn tại");
            if (soTienMuonDong <= 0) throw new Exception("Số tiền đóng phải > 0");

            // Lấy ví (tk)
            var tk = await GetOrCreateTaiKhoanAsync(hoaDon.SinhVienId);

            // Kiểm tra số dư ví
            if (tk.SoDu < soTienMuonDong)
            {
                throw new Exception($"Số dư ví không đủ ({tk.SoDu:N0}đ). Cần nạp thêm tiền.");
            }

            // Kiểm tra số nợ thực tế
            decimal noThucTe = hoaDon.TongTien - hoaDon.DaDong;
            if (soTienMuonDong > noThucTe)
            {
                throw new Exception($"Số tiền đóng ({soTienMuonDong:N0}đ) vượt quá số nợ còn lại ({noThucTe:N0}đ).");
            }

            // Tạo giao dịch THANH TOÁN (Trừ tiền)
            var gd = new GiaoDichThanhToan
            {
                TaiKhoanSinhVienId = tk.Id,
                HoaDonHocPhiId = hoaDonId,
                SoTien = -soTienMuonDong, // Số âm
                LoaiGiaoDich = 2, // 2 = Thanh toán học phí
                HinhThuc = "TruVi",
                NguoiThucHien = adminUser,
                NgayThanhToan = DateTime.Now,
                GhiChu = $"Thanh toán học phí HK {hoaDon.HocKy} - {hoaDon.NamHoc}"
            };
            _context.GiaoDichThanhToans.Add(gd);

            // Trừ số dư ví (Sửa lỗi logic cũ)
            tk.SoDu -= soTienMuonDong;
            tk.NgayCapNhatCuoi = DateTime.Now;

            // Cập nhật Hóa đơn
            hoaDon.DaDong += soTienMuonDong;
            hoaDon.ConLai = hoaDon.TongTien - hoaDon.DaDong;

            // Cập nhật trạng thái
            if (hoaDon.ConLai <= 0 && hoaDon.TongTien > 0)
                hoaDon.TrangThaiId = sttHoanThanh;
            else if (hoaDon.DaDong > 0)
                hoaDon.TrangThaiId = sttMotPhan;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi SaveChangesAsync: " + ex.Message, ex);
            }

            // Log với thông tin lấy từ hóa đơn
            string mssv = hoaDon.SinhVien?.MSSV ?? "Unknown";
            await _activityLogService.LogAsync(null, adminUser, "System", "::1", "PAYMENT", "HoaDonHocPhi", hoaDon.Id.ToString(), $"Thanh toán {soTienMuonDong:N0}đ từ ví cho SV {mssv}.");
        }

        private int GetHocKyNumber(string tenHocKy)
        {
            if (string.IsNullOrEmpty(tenHocKy)) return 1;
            if (tenHocKy.Contains("1")) return 1;
            if (tenHocKy.Contains("2")) return 2;
            if (tenHocKy.Contains("3") || tenHocKy.Contains("Hè")) return 3;
            return 1;
        }
    }
}