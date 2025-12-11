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

        // Bỏ IActivityLogService trong constructor vì không dùng nữa
        public HocPhiService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =================================================================================
        // PHẦN 1: CÁC HÀM HELPER (Private)
        // =================================================================================

        private async Task<HocPhi> GetOrCreateHocPhiAsync(int sinhVienId)
        {
            var hocPhi = await _context.HocPhis
                .FirstOrDefaultAsync(hp => hp.SinhVienId == sinhVienId);

            if (hocPhi == null)
            {
                hocPhi = new HocPhi
                {
                    SinhVienId = sinhVienId,
                    DuNoConLai = 0,
                    NgayCapNhatCuoi = DateTime.Now
                };
                _context.HocPhis.Add(hocPhi);
                await _context.SaveChangesAsync();
            }
            return hocPhi;
        }

        private async Task<decimal> GetDonGiaTinChiAsync(int hocKyId, int nganhId)
        {
            var hk = await _context.HocKys.FindAsync(hocKyId);
            if (hk == null) return 0;

            int startYear = hk.NgayBatDau.Year;
            if (hk.NgayBatDau.Month < 6) startYear--;
            string namHocStr = $"{startYear}-{startYear + 1}";

            var dinhMuc = await _context.DinhMucHocPhis
                .Where(d => d.NamHoc == namHocStr && d.NganhId == nganhId)
                .FirstOrDefaultAsync();

            if (dinhMuc == null)
            {
                dinhMuc = await _context.DinhMucHocPhis
                    .Where(d => d.NamHoc == namHocStr && d.NganhId == null)
                    .FirstOrDefaultAsync();
            }

            return dinhMuc?.GiaTienMotTinChi ?? 0;
        }

        // =================================================================================
        // PHẦN 2: NGHIỆP VỤ TÍNH TOÁN
        // =================================================================================

        // GHI NỢ (Không cần tham số adminUser nữa)
        public async Task GhiNoHocPhiAsync(int dangKyHocPhanId)
        {
            var dk = await _context.DangKyHocPhans
                .Include(d => d.SinhVien).ThenInclude(sv => sv.Lop)
                .Include(d => d.LopHocPhan).ThenInclude(l => l.MonHoc)
                .FirstOrDefaultAsync(d => d.Id == dangKyHocPhanId);

            if (dk == null) return;
            if (dk.SinhVien.Lop == null) return; // Data lỗi thì bỏ qua

            // 1. Tính tiền
            decimal donGia = await GetDonGiaTinChiAsync(dk.LopHocPhan.HocKyId, dk.SinhVien.Lop.NganhId ?? 0);
            if (donGia <= 0) return;

            decimal thanhTien = donGia * dk.LopHocPhan.MonHoc.SoTinChi;

            // 2. Lấy sổ cái
            var hoSo = await GetOrCreateHocPhiAsync(dk.SinhVienId);

            // 3. Tạo dòng chi tiết
            var chiTiet = new ChiTietHocPhi
            {
                HocPhiId = hoSo.Id,
                DangKyHocPhanId = dk.Id,
                SoTien = thanhTien,
                NgayPhatSinh = DateTime.Now
            };
            _context.ChiTietHocPhis.Add(chiTiet);

            // 4. Cộng nợ
            hoSo.DuNoConLai += thanhTien;
            hoSo.NgayCapNhatCuoi = DateTime.Now;

            await _context.SaveChangesAsync();
            // ĐÃ XÓA LOG
        }

        // HỦY NỢ
        public async Task HuyNoHocPhiAsync(int dangKyHocPhanId)
        {
            var chiTiet = await _context.ChiTietHocPhis
                .Include(ct => ct.HocPhi)
                .FirstOrDefaultAsync(ct => ct.DangKyHocPhanId == dangKyHocPhanId);

            if (chiTiet != null)
            {
                decimal tienHuy = chiTiet.SoTien;

                if (chiTiet.HocPhi != null)
                {
                    chiTiet.HocPhi.DuNoConLai -= tienHuy;
                    chiTiet.HocPhi.NgayCapNhatCuoi = DateTime.Now;
                }

                _context.ChiTietHocPhis.Remove(chiTiet);
                await _context.SaveChangesAsync();
                // ĐÃ XÓA LOG
            }
        }

        // THANH TOÁN
        public async Task NopTienHocPhiAsync(int sinhVienId, decimal soTien, string nguoiThu, string ghiChu)
        {
            if (soTien <= 0) throw new Exception("Số tiền nộp phải lớn hơn 0.");

            var hoSo = await GetOrCreateHocPhiAsync(sinhVienId);

            var phieu = new PhieuThu
            {
                SinhVienId = sinhVienId,
                SoTienDong = soTien,
                NguoiThu = nguoiThu,
                GhiChu = ghiChu,
                NgayDong = DateTime.Now
            };
            _context.PhieuThus.Add(phieu);

            hoSo.DuNoConLai -= soTien;
            hoSo.NgayCapNhatCuoi = DateTime.Now;

            await _context.SaveChangesAsync();
            // ĐÃ XÓA LOG
        }

        // =================================================================================
        // PHẦN 3: QUERY DỮ LIỆU
        // =================================================================================

        public async Task<List<HocPhiDashboardVM>> GetDanhSachCongNoAsync(string? keyword, int? lopId)
        {
            var query = _context.SinhViens.Include(sv => sv.Lop).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(sv => sv.MSSV.Contains(keyword) || (sv.HoVaTenDem + " " + sv.Ten).Contains(keyword));

            if (lopId.HasValue)
                query = query.Where(sv => sv.LopId == lopId);

            return await query.Select(sv => new HocPhiDashboardVM
            {
                SinhVienId = sv.Id,
                MSSV = sv.MSSV,
                HoTen = sv.HoVaTenDem + " " + sv.Ten,
                TenLop = sv.Lop != null ? sv.Lop.MaLop : "N/A",
                TongNo = _context.HocPhis.Where(hp => hp.SinhVienId == sv.Id).Select(hp => hp.DuNoConLai).FirstOrDefault()
            }).ToListAsync();
        }

        public async Task<HocPhiViewModel> GetThongTinHocPhi(int sinhVienId)
        {
            var hp = await GetOrCreateHocPhiAsync(sinhVienId);

            // 1. Lấy danh sách nợ (Chi tiết môn học)
            var listNoRaw = await _context.ChiTietHocPhis
                .Include(ct => ct.DangKyHocPhan)
                    .ThenInclude(dk => dk.LopHocPhan)
                        .ThenInclude(lhp => lhp.MonHoc)
                .Include(ct => ct.DangKyHocPhan)
                    .ThenInclude(dk => dk.LopHocPhan)
                        .ThenInclude(lhp => lhp.HocKy)
                .Where(ct => ct.HocPhiId == hp.Id)
                .OrderByDescending(ct => ct.NgayPhatSinh)
                .ToListAsync();

            // 2. Nhóm theo Học kỳ
            var danhSachTheoKy = listNoRaw
                .GroupBy(x => new {
                    Id = x.DangKyHocPhan?.LopHocPhan?.HocKyId ?? 0,
                    Ten = x.DangKyHocPhan?.LopHocPhan?.HocKy?.TenHocKy ?? "Khác"
                })
                .Select(g => new HocPhiTheoKyVM
                {
                    HocKyId = g.Key.Id,
                    TenHocKy = g.Key.Ten,
                    ChiTietMonHoc = g.Select(ct => new ChiTietHocPhiVM
                    {
                        NgayDK = ct.NgayPhatSinh,
                        MaMonHoc = ct.DangKyHocPhan?.LopHocPhan?.MonHoc?.MaMonHoc ?? "",
                        TenMonHoc = ct.DangKyHocPhan?.LopHocPhan?.MonHoc?.TenMonHoc ?? ct.DangKyHocPhan?.LoaiDangKy ?? "Phí khác",
                        TenLopHP = ct.DangKyHocPhan?.LopHocPhan?.MaLopHocPhan ?? "",
                        SoTinChi = ct.DangKyHocPhan?.LopHocPhan?.MonHoc?.SoTinChi ?? 0,
                        SoTien = ct.SoTien
                    }).ToList()
                })
                .OrderByDescending(k => k.HocKyId) // Kỳ mới nhất lên đầu
                .ToList();

            // 3. Lấy lịch sử đóng tiền
            var listDong = await _context.PhieuThus
                .Where(p => p.SinhVienId == sinhVienId)
                .OrderByDescending(p => p.NgayDong)
                .ToListAsync();

            return new HocPhiViewModel
            {
                DuNoHienTai = hp.DuNoConLai,
                DanhSachHocKy = danhSachTheoKy,
                LichSuPhatSinh = listNoRaw,
                LichSuDongTien = listDong
            };
        }
    }
}