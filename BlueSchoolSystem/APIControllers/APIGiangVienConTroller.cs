using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Services;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APIGiangVienConTroller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public APIGiangVienConTroller(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //Lấy danh sách giảng viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachgiangvien")]
        public async Task<IActionResult> GetAllGiangVien()
        {
            var giangviens = await _context.GiangViens
                        .Include(gv => gv.Khoa)
                        .Include(tt => tt.TrangThai)
                        .OrderBy(gv => gv.Id)
                        .ToListAsync();
            var tonggiangvien = await _context.GiangViens.CountAsync();
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                soluonggiangvien = tonggiangvien,
                data = giangviens
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("timkiemgiangvien")]
        public async Task<IActionResult> TimKiemGiangVien(string? keyword, string? maKhoa, int? trangThaiId)
        {
            var query = _context.GiangViens
                .Include(gv => gv.Khoa)
                .Include(gv => gv.TrangThai)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(gv =>
                    gv.MaGiangVien.ToLower().Contains(kw) ||
                    gv.HoVaTenDem.ToLower().Contains(kw) ||
                    gv.Ten.ToLower().Contains(kw));
            }

            if (!string.IsNullOrEmpty(maKhoa))
            {
                query = query.Where(gv => gv.Khoa != null && gv.Khoa.MaKhoa == maKhoa);
            }

            if (trangThaiId.HasValue && trangThaiId.Value > 0)
            {
                query = query.Where(gv => gv.TrangThaiId == trangThaiId.Value);
            }

            var data = await query.OrderBy(gv => gv.Id).ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                soluonggiangvien = data.Count,
                data = data
            });
        }

        //Thêm giảng viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("themgiangvien")]
        public async Task<IActionResult> AddGiangVien([FromBody] CreateGiangVienWithUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName)
                || string.IsNullOrWhiteSpace(request.Email)
                || string.IsNullOrWhiteSpace(request.Password)
                || request.GiangVien == null
                || string.IsNullOrWhiteSpace(request.GiangVien.HoVaTenDem)
                || string.IsNullOrWhiteSpace(request.GiangVien.Ten)
                || string.IsNullOrWhiteSpace(request.GiangVien.CCCD)
                || string.IsNullOrWhiteSpace(request.GiangVien.DiaChi)
                || request.GiangVien.KhoaId == null
                || request.GiangVien.NgaySinh == DateTime.MinValue
            )
            {
                return BadRequest("Thiếu thông tin bắt buộc. Dữ liệu không hợp lệ.");
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            // 1. Tạo user
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, SD.Role_Teacher);

            // 2. Gán UserId vào giảng viên
            var giangvien = request.GiangVien;
            giangvien.UserId = user.Id;
            giangvien.CreatedAt = DateTime.Now;
            giangvien.UpdatedAt = DateTime.Now;

            // 3. Lưu vào DB
            _context.GiangViens.Add(giangvien);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo giảng viên và tài khoản thành công!",
                giangvienId = giangvien.Id,
                userId = user.Id
            });
        }

        //Sửa thông tin giảng viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("suathongtingiangvien/{maGiangVien}")]
        public async Task<IActionResult> UpdateGiangVien(string maGiangVien, [FromBody] UpdateGiangVienRequest model)
        {
            var giangVien = await _context.GiangViens
                .Include(gv => gv.User)
                .FirstOrDefaultAsync(gv => gv.MaGiangVien == maGiangVien);

            if (giangVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy giảng viên." });

            // Update từng field nếu truyền vào (patch)
            if (!string.IsNullOrEmpty(model.HoVaTenDem)) giangVien.HoVaTenDem = model.HoVaTenDem;
            if (!string.IsNullOrEmpty(model.Ten)) giangVien.Ten = model.Ten;
            if (model.GioiTinh != null) giangVien.GioiTinh = model.GioiTinh.Value;
            if (model.NgaySinh != null) giangVien.NgaySinh = model.NgaySinh.Value;
            if (!string.IsNullOrEmpty(model.CCCD)) giangVien.CCCD = model.CCCD;
            if (!string.IsNullOrEmpty(model.DiaChi)) giangVien.DiaChi = model.DiaChi;
            if (model.TrangThaiId != null) giangVien.TrangThaiId = model.TrangThaiId.Value;
            if (!string.IsNullOrEmpty(model.GhiChu)) giangVien.GhiChu = model.GhiChu;
            giangVien.UpdatedAt = DateTime.Now;

            // Update user info nếu có
            if (giangVien.User != null)
            {
                if (!string.IsNullOrEmpty(model.Email)) giangVien.User.Email = model.Email;
                if (!string.IsNullOrEmpty(model.PhoneNumber)) giangVien.User.PhoneNumber = model.PhoneNumber;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new 
                {
                    result = true,
                    code = 200,
                    message = "Cập nhật thông tin Cán bộ - Giảng viên thành công!",
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Có thể ghi log dbEx.Message vô file/server nếu muốn
                return BadRequest(new { result = false, message = "Không thể cập nhật dữ liệu. Vui lòng thử lại!" });
            }
            catch (Exception ex)
            {
                // Ghi log ex.Message nếu cần
                return StatusCode(500, new { result = false, message = "Lỗi hệ thống! " + ex.Message });
            }
        }


        //Lấy thông tin chi tiết của giảng viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("chitietgiangvien/{maGV}")]
        public async Task<IActionResult> GetGiangVienDetailByMa(string maGV)
        {
            try
            {
                var gv = await _context.GiangViens
                    .Include(x => x.User)
                    .Include(x => x.Khoa)
                    .Include(x => x.TrangThai)
                    .FirstOrDefaultAsync(x => x.MaGiangVien.ToLower() == maGV.ToLower());

                if (gv == null)
                {
                    return NotFound(new
                    {
                        result = false,
                        message = "Không tìm thấy giảng viên với mã: " + maGV
                    });
                }

                var result = new
                {
                    result = true,
                    data = new
                    {
                        gv.Id,
                        gv.MaGiangVien,
                        gv.HoVaTenDem,
                        gv.Ten,
                        gv.CCCD,
                        gv.NgaySinh,
                        gv.GioiTinh,
                        gv.DiaChi,
                        gv.TrangThaiId,
                        TrangThai = gv.TrangThai != null ? new { gv.TrangThai.Id, gv.TrangThai.TenTrangThai } : null,
                        gv.GhiChu,
                        gv.AvatarUrl,
                        gv.KhoaId,
                        Khoa = gv.Khoa != null ? new { gv.Khoa.Id, gv.Khoa.TenKhoa } : null,
                        User = gv.User != null ? new
                        {
                            gv.User.Id,
                            gv.User.UserName,
                            gv.User.Email,
                            gv.User.PhoneNumber
                        } : null,
                        gv.CreatedAt,
                        gv.UpdatedAt
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Ghi log nếu muốn
                return StatusCode(500, new
                {
                    result = false,
                    message = "Đã xảy ra lỗi khi lấy thông tin giảng viên.",
                    error = ex.Message
                });
            }
        }


        // Lấy danh sách lịch giảng dạy theo mã giảng viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("lichgiangday/{magv}")]
        public async Task<IActionResult> GetLichGiangDayByMaGV(string magv)
        {
            // 🔹 Lấy mã giảng viên từ JWT claim
            var magvFromToken = User.FindFirst("username")?.Value;

            if (magvFromToken == null)
            {
                return Unauthorized(new
                {
                    result = false,
                    code = 401,
                    message = "Không lấy được mã giảng viên từ token"
                });
            }

            // 🔹 Giảng viên chỉ được xem lịch của chính mình (trừ admin)
            var isAdmin = User.IsInRole(SD.Role_Admin);
            if (!isAdmin && magvFromToken != magv)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    result = false,
                    code = 403,
                    message = "Bạn không có quyền truy cập lịch giảng dạy của giảng viên khác"
                });
            }

            // 🔹 Lấy danh sách lịch giảng dạy (join LichHoc, LopHocPhan, MonHoc, PhongHoc)
            var lichData = await (
                from lhp in _context.LopHocPhans
                join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                join gv in _context.GiangViens on lhp.GiangVienId equals gv.Id
                join lh in _context.LichHocs on lhp.Id equals lh.LopHocPhanId
                join ph in _context.PhongHocs on lh.PhongHocId equals ph.Id into gph
                from ph in gph.DefaultIfEmpty() // cho phép null
                where gv.MaGiangVien == magv
                orderby lh.Ngay, lh.GioBatDau
                select new
                {
                    lhp.MaLopHocPhan,
                    //lhp.TenLopHocPhan,
                    MaMonHoc = mh.MaMonHoc,
                    TenMonHoc = mh.TenMonHoc,
                    MaPhongHoc = ph != null ? ph.MaPhongHoc : "Chưa có phòng",
                    lh.Ngay,
                    lh.GioBatDau,
                    lh.GioKetThuc,
                    lhp.NgayBatDau,
                    lhp.NgayKetThuc,
                    SoLuongSinhVien = _context.ChiTietLopHocPhans.Count(ct => ct.LopHocPhanId == lhp.Id)
                }
            ).ToListAsync();

            if (!lichData.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lịch giảng dạy cho giảng viên này"
                });
            }

            // 🔹 Tính tiết bắt đầu và số tiết
            int ToTiet(TimeSpan gio)
            {
                if (gio <= TimeSpan.Parse("6:45")) return 1;
                if (gio <= TimeSpan.Parse("07:30")) return 2;
                if (gio <= TimeSpan.Parse("08:15")) return 3;
                if (gio <= TimeSpan.Parse("09:20")) return 4;
                if (gio <= TimeSpan.Parse("10:05")) return 5;
                if (gio <= TimeSpan.Parse("10:50")) return 6;
                if (gio <= TimeSpan.Parse("12:30")) return 7;
                if (gio <= TimeSpan.Parse("13:10")) return 8;
                if (gio <= TimeSpan.Parse("14:00")) return 9;
                if (gio <= TimeSpan.Parse("15:05")) return 10;
                if (gio <= TimeSpan.Parse("15:50")) return 11;
                if (gio <= TimeSpan.Parse("16:35")) return 12;
                if (gio <= TimeSpan.Parse("18:00")) return 13;
                if (gio <= TimeSpan.Parse("18:45")) return 14;
                return 15;
            }

            var lichGiangDay = lichData.Select(item =>
            {
                int tietBatDau = ToTiet(item.GioBatDau);
                int tietKetThuc = ToTiet(item.GioKetThuc);
                int soTiet = tietKetThuc - tietBatDau;

                return new
                {
                    item.MaLopHocPhan,
                    item.MaMonHoc,
                    item.TenMonHoc,
                    item.MaPhongHoc,
                    item.Ngay,
                    GioBatDau = item.GioBatDau.ToString(@"hh\:mm"),
                    GioKetThuc = item.GioKetThuc.ToString(@"hh\:mm"),
                    item.NgayBatDau,
                    item.NgayKetThuc,
                    TietBatDau = tietBatDau,
                    SoTiet = soTiet,
                    item.SoLuongSinhVien
                };
            }).ToList();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy lịch giảng dạy thành công",
                soluong = lichGiangDay.Count,
                data = lichGiangDay
            });
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("lophocphan")]
        public async Task<IActionResult> LayLopHP()
        {
            // Lấy userId từ token
            var userId = User.FindFirst("userId")?.Value;
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.UserId == userId);
            if (giangVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy thông tin giảng viên" });

            // JOIN LopHocPhan với HocKy, rồi group by học kỳ
            var query = from lhp in _context.LopHocPhans
                        join hk in _context.HocKys on lhp.HocKyId equals hk.Id
                        join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                        where lhp.GiangVienId == giangVien.Id
                        select new
                        {
                            lhp.Id,
                            lhp.MaLopHocPhan,
                            lhp.TenLopHocPhan,
                            mh.MaMonHoc,
                            mh.TenMonHoc,
                            NgayBatDauLop = lhp.NgayBatDau,
                            NgayKetThucLop = lhp.NgayKetThuc,
                            SiSoThucTe = _context.ChiTietLopHocPhans.Count(ct => ct.LopHocPhanId == lhp.Id),
                            lhp.TrangThai,
                            HocKyId = hk.Id,
                            hk.TenHocKy,
                            hk.NgayBatDau,
                        };

            var result = await query
                .GroupBy(x => new { x.HocKyId, x.TenHocKy, x.NgayBatDau })
                .Select(g => new
                {
                    HocKyId = g.Key.HocKyId,
                    TenHocKy = g.Key.TenHocKy,
                    NgayBatDau = g.Key.NgayBatDau,
                    LopHocPhans = g.OrderBy(x => x.MaLopHocPhan).ToList()
                })
                .OrderByDescending(x => x.NgayBatDau)
                .ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách học phần thành công",
                soluong = result.Count,
                data = result
            });
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("chitietlophocphan/{maLopHocPhan}")]
        public async Task<IActionResult> LayChiTietLopHocPhan(string maLopHocPhan)
        {
            // Truy vấn lớp học phần theo mã lớp
            var lop = await (from lhp in _context.LopHocPhans
                             join hk in _context.HocKys on lhp.HocKyId equals hk.Id
                             join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                             join ct in _context.ChiTietLopHocPhans on lhp.Id equals ct.LopHocPhanId into _ct
                             where lhp.MaLopHocPhan == maLopHocPhan
                             select new
                             {
                                 lhp.Id,
                                 lhp.MaLopHocPhan,
                                 lhp.TenLopHocPhan,
                                 mh.MaMonHoc,
                                 mh.TenMonHoc,
                                 hk.TenHocKy,
                                 hk.NgayBatDau,
                                 hk.NgayKetThuc,
                                 lhp.TrangThai,
                                 // Sĩ số thực tế
                                 SiSoThucTe = _context.ChiTietLopHocPhans.Count(x => x.LopHocPhanId == lhp.Id),
                                 // Danh sách sinh viên
                                 DanhSachSinhVien = (from ct in _context.ChiTietLopHocPhans
                                                     join sv in _context.SinhViens on ct.SinhVienId equals sv.Id
                                                     where ct.LopHocPhanId == lhp.Id
                                                     select new
                                                     {
                                                         sv.MSSV,
                                                         sv.HoVaTenDem,
                                                         sv.Ten,
                                                     }).ToList()
                             }).FirstOrDefaultAsync();

            if (lop == null)
                return NotFound(new { result = false, message = "Không tìm thấy lớp học phần với mã này!" });

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy chi tiết lớp học phần thành công",
                data = lop
            });
        }



        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("lophocphan/{lopHocPhanId}/buoidiemdanh")]
        public async Task<IActionResult> GetDiemDanhByLopHocPhan(int lopHocPhanId)
        {
            var userId = User.FindFirst("userId")?.Value;
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.UserId == userId);
            if (giangVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy thông tin giảng viên" });

            var lopHocPhan = await _context.LopHocPhans.FirstOrDefaultAsync(lhp => lhp.Id == lopHocPhanId);
            if (lopHocPhan == null || lopHocPhan.GiangVienId != giangVien.Id)
                return StatusCode(403, new { result = false, message = "Không có quyền truy cập lớp học phần này" });

            var buois = await _context.DiemDanhs
                .Where(dd => dd.LopHocPhanId == lopHocPhanId)
                .OrderByDescending(dd => dd.Ngay)
                .Select(dd => new {
                    dd.Id,
                    dd.Ngay,
                    dd.Code,
                    dd.CreatedAt,
                    dd.ExpireAt,
                    dd.GhiChu,
                    dd.TrangThaiId
                })
                .ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách buổi điểm danh thành công",
                soluong = buois.Count,
                data = buois
            });
        }

        public class CreateDiemDanhRequest
        {
            public DateTime Ngay { get; set; }
            public string? GhiChu { get; set; }
            public DateTime? ExpireAt { get; set; }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpPost("lophocphan/{lopHocPhanId}/buoidiemdanh/tao")]
        public async Task<IActionResult> TaoBuoiDiemDanh(int lopHocPhanId, [FromBody] CreateDiemDanhRequest model)
        {
            var userId = User.FindFirst("userId")?.Value;
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.UserId == userId);
            if (giangVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy thông tin giảng viên" });

            var lopHocPhan = await _context.LopHocPhans.FirstOrDefaultAsync(lhp => lhp.Id == lopHocPhanId);
            if (lopHocPhan == null || lopHocPhan.GiangVienId != giangVien.Id)
                return StatusCode(403, new { result = false, message = "Không có quyền truy cập lớp học phần này" });


            // Kiểm tra lịch học đã khai báo cho ngày tạo buổi
            var coLichDay = await _context.LichHocs
                .AnyAsync(lh =>
                    lh.LopHocPhanId == lopHocPhanId
                    && lh.Ngay.Date == model.Ngay.Date
                );

            if (!coLichDay)
            {
                return BadRequest(new { result = false, message = "Bạn chỉ có thể tạo được buổi điểm danh vào ngày có lịch giảng dạy của môn này." });
            }

            var danhSachSinhVien = await _context.ChiTietLopHocPhans
                .Where(ct => ct.LopHocPhanId == lopHocPhanId)
                .Select(ct => new
                {
                    SinhVienId = ct.SinhVienId,
                    MSSV = ct.SinhVien.MSSV,
                    HoTen = (
                        (ct.SinhVien.HoVaTenDem ?? "") + " " + (ct.SinhVien.Ten ?? "")
                    ).Trim()
                })
                .AsNoTracking()
                .OrderBy(x => x.HoTen)
                .ToListAsync();


            if (!danhSachSinhVien.Any())
                return BadRequest(new { result = false, message = "Lớp chưa có danh sách sinh viên, không thể tạo buổi điểm danh." });



            // Sinh mã code ngắn gọn
            string code;
            var random = new Random();
            bool exists;
            int maxTry = 100; // thử tối đa 100 lần

            do
            {
                code = random.Next(1000, 10000).ToString(); // 4 số (1000 -> 9999)
                exists = await _context.DiemDanhs.AnyAsync(dd =>
                    dd.ExpireAt >= DateTime.Now && dd.Code == code
                );
                maxTry--;
            } while (exists && maxTry > 0);

            if (exists)
            {
                return BadRequest(new { result = false, message = "Không tạo được mã điểm danh, thử lại sau!" });
            }

            var trangThaiBuoiDiemDanhId = _context.TrangThais
            .Where(t => t.LoaiTrangThai == "DiemDanh#" && t.TenTrangThai == "Đang diễn ra")
            .Select(t => t.Id)
            .FirstOrDefault();

            var trangThaiChuaDiemDanhId = await _context.TrangThais
            .Where(t => t.LoaiTrangThai == "DiemDanh" && t.TenTrangThai == "Vắng mặt")
            .Select(t => t.Id)
            .FirstOrDefaultAsync();

            if (trangThaiBuoiDiemDanhId == 0 || trangThaiChuaDiemDanhId == 0)
                return BadRequest(new { result = false, message = "Thiếu cấu hình trạng thái điểm danh" });


            var buoi = new DiemDanh
            {
                LopHocPhanId = lopHocPhanId,
                Ngay = model.Ngay,
                Code = code,
                CreatedAt = DateTime.Now,
                ExpireAt = model.ExpireAt ?? DateTime.Now.AddMinutes(20),
                GhiChu = model.GhiChu,
                TrangThaiId = trangThaiBuoiDiemDanhId,
            };
            _context.DiemDanhs.Add(buoi);
            await _context.SaveChangesAsync();

            var details = danhSachSinhVien.Select(x => new ChiTietDiemDanh
            {
                DiemDanhId = buoi.Id,
                SinhVienId = (int)x.SinhVienId,
                TrangThaiId = trangThaiChuaDiemDanhId,
                ThoiGian = DateTime.MinValue 
            });

            _context.ChiTietDiemDanhs.AddRange(details);
            await _context.SaveChangesAsync();

            // Lấy mã LHP + danh sách SV để seed
            var maLop = lopHocPhan.MaLopHocPhan;
            var danhSachHoTen = danhSachSinhVien.Select(x => (x.MSSV, x.HoTen));
            await PushSessionSeedToFirebase(buoi, maLop, danhSachHoTen, trangThaiChuaDiemDanhId);


            return Ok(new
            {
                result = true,
                code = 200,
                message = "Tạo buổi điểm danh thành công",
                data = new
                {
                    buoi.Id,
                    buoi.Ngay,
                    buoi.Code,
                    buoi.CreatedAt,
                    buoi.ExpireAt,
                    buoi.GhiChu,
                    buoi.TrangThaiId
                }
            });
        }

        private async Task PushSessionSeedToFirebase(
        DiemDanh buoi,
        string maLopHocPhan,
        IEnumerable<(string MSSV, string HoTen)> sinhViens,
        int trangThaiVangId)
        {
            // NOTE: dùng path "attendancesessions" để khớp Flutter
            var fb = new Firebase.Database.FirebaseClient("https://bluenet-e6525-default-rtdb.firebaseio.com");

            // 1) push metadata buổi
            var meta = new
            {
                code = buoi.Code,
                maLopHocPhan = maLopHocPhan,
                ngay = buoi.Ngay.ToString("yyyy-MM-dd"),
                createdAt = buoi.CreatedAt.ToString("s"),
                expireAt = buoi.ExpireAt.ToString("s")
            };
            await fb.Child("attendancesessions")
                    .Child(buoi.Id.ToString())
                    .PatchAsync(meta);

            // 2) seed students map
            var students = new Dictionary<string, object>();
            foreach (var sv in sinhViens)
            {
                students[sv.MSSV] = new
                {
                    studentId = sv.MSSV,
                    studentName = sv.HoTen,
                    status = "Vắng mặt",               
                    statusId = trangThaiVangId,    
                    timecheckedin = (string?)null,
                    bluetoothID = (string?)null
                };
            }

            await fb.Child("attendancesessions")
                    .Child(buoi.Id.ToString())
                    .Child("students")
                    .PutAsync(students);
        }



        // GET: api/buoidiemdanh/{diemDanhId}/chitiet
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("buoidiemdanh/{diemDanhId}/chitiet")]
        public async Task<IActionResult> GetChiTietBuoiDiemDanh(int diemDanhId)
        {
            var userId = User.FindFirst("userId")?.Value;
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.UserId == userId);
            if (giangVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy thông tin giảng viên" });

            var buoi = await _context.DiemDanhs
                .Include(x => x.LopHocPhan)
                .FirstOrDefaultAsync(x => x.Id == diemDanhId);

            if (buoi == null || buoi.LopHocPhan.GiangVienId != giangVien.Id)
                return StatusCode(403, new { result = false, message = "Không có quyền truy cập buổi điểm danh này" });

            // Lấy danh sách sinh viên điểm danh
            var svIds = await _context.ChiTietLopHocPhans
                .Where(ct => ct.LopHocPhanId == buoi.LopHocPhanId)
                .Select(ct => ct.SinhVienId)
                .ToListAsync();

            var sinhViens = await _context.ChiTietDiemDanhs
                .Where(ctdd => ctdd.DiemDanhId == diemDanhId)
                .OrderBy(ctdd => ctdd.SinhVien.MSSV)
                .Select(ctdd => new
                {
                    ctdd.SinhVien.Id,
                    ctdd.SinhVien.MSSV,
                    ctdd.SinhVien.HoVaTenDem,
                    ctdd.SinhVien.Ten,
                    TrangThai = ctdd.TrangThaiId,
                    ThoiGian = ctdd.ThoiGian
                })
                .ToListAsync();

            var result = new
            {
                buoi.Id,
                buoi.Code,
                buoi.Ngay,          
                buoi.ExpireAt,
                MaLopHocPhan = buoi.LopHocPhan.MaLopHocPhan,
                SinhViens = sinhViens
            };


            return Ok(new
            {
                result = true,
                data = result
            });
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("lophocphan/ma/{maLopHocPhan}/buoidiemdanh")]
        public async Task<IActionResult> GetDiemDanhByMaLopHocPhan(string maLopHocPhan)
        {
            var userId = User.FindFirst("userId")?.Value;
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.UserId == userId);
            if (giangVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy thông tin giảng viên" });

            var lopHocPhan = await _context.LopHocPhans.FirstOrDefaultAsync(lhp => lhp.MaLopHocPhan == maLopHocPhan);
            if (lopHocPhan == null || lopHocPhan.GiangVienId != giangVien.Id)
                return StatusCode(403, new { result = false, message = "Không có quyền truy cập lớp học phần này" });

            var buois = await _context.DiemDanhs
                .Where(dd => dd.LopHocPhanId == lopHocPhan.Id)
                .OrderByDescending(dd => dd.Ngay)
                .Select(dd => new {
                    dd.Id,
                    dd.Ngay,
                    dd.Code,
                    dd.CreatedAt,
                    dd.ExpireAt,
                    dd.GhiChu,
                    dd.TrangThaiId
                })
                .ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách buổi điểm danh thành công",
                soluong = buois.Count,
                data = buois
            });
        }

        [HttpPost("buoidiemdanh/{id}/regeneratecode")]
        public async Task<IActionResult> RegenerateCode(int id)
        {
            var buoi = await _context.DiemDanhs.FindAsync(id);
            if (buoi == null)
                return NotFound();

            // Tạo lại code mới, đảm bảo không trùng
            string code;
            var random = new Random();
            int maxTry = 100;
            bool exists;
            do
            {
                code = random.Next(1000, 10000).ToString();
                exists = await _context.DiemDanhs.AnyAsync(dd => dd.ExpireAt >= DateTime.Now && dd.Code == code);
                maxTry--;
            } while (exists && maxTry > 0);

            if (exists)
                return BadRequest(new { result = false, message = "Không tạo được mã mới" });

            buoi.Code = code;
            buoi.ExpireAt = DateTime.Now.AddSeconds(60); // reset expire mới nếu muốn
            await _context.SaveChangesAsync();

            return Ok(new { result = true, newCode = code });
        }

        [HttpPost("diemdanh/capnhattrangthai")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
           Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        public async Task<IActionResult> UpdateTrangThaiDiemDanh([FromBody] UpdateTrangThaiModel model)
        {
            if (model.DiemDanhId <= 0)
                return BadRequest(new { result = false, message = "Thiếu DiemDanhId" });

            // tìm SV theo Id hoặc MSSV
            var sv = model.SinhVienId > 0
                ? await _context.SinhViens.FindAsync(model.SinhVienId)
                : !string.IsNullOrWhiteSpace(model.MSSV)
                    ? await _context.SinhViens.FirstOrDefaultAsync(x => x.MSSV == model.MSSV)
                    : null;

            if (sv == null)
                return NotFound(new { result = false, message = "Không tìm thấy sinh viên" });

            // map trạng thái: ưu tiên id, fallback theo text ("Có mặt", "Đi trễ", "Vắng", "Vắng có phép")
            int trangThaiId = 0;
            if (model.TrangThaiId.HasValue) trangThaiId = model.TrangThaiId.Value;
            else if (!string.IsNullOrWhiteSpace(model.TrangThaiText))
            {
                trangThaiId = await _context.TrangThais
                    .Where(t => t.LoaiTrangThai == "DiemDanh" && t.TenTrangThai == model.TrangThaiText)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();
            }
            if (trangThaiId == 0)
                return BadRequest(new { result = false, message = "Trạng thái không hợp lệ" });

            // upsert ChiTietDiemDanh
            var ct = await _context.ChiTietDiemDanhs
                .FirstOrDefaultAsync(x => x.DiemDanhId == model.DiemDanhId && x.SinhVienId == sv.Id);

            if (ct == null)
            {
                ct = new ChiTietDiemDanh
                {
                    DiemDanhId = model.DiemDanhId,
                    SinhVienId = sv.Id
                };
                _context.ChiTietDiemDanhs.Add(ct);
            }

            ct.TrangThaiId = trangThaiId;
            ct.ThoiGian = DateTime.Now;
            ct.GhiChu = model.Source ?? "Manual/BLE";

            await _context.SaveChangesAsync();

            // lấy text trạng thái để đẩy Firebase cho app
            var statusText = await _context.TrangThais
                .Where(t => t.Id == trangThaiId)
                .Select(t => t.TenTrangThai)
                .FirstOrDefaultAsync() ?? "Không rõ";

            // push lên Realtime DB (đã có helper này trước đó)
            await PushStatusToFirebaseBoth(
                model.DiemDanhId,
                sv.MSSV,
                $"{sv.HoVaTenDem} {sv.Ten}".Trim(),
                trangThaiId,
                statusText,
                ct.ThoiGian
            );

            return Ok(new { result = true });
        }

        public class UpdateTrangThaiModel
        {
            public int DiemDanhId { get; set; }
            public int? SinhVienId { get; set; }         // optional
            public string? MSSV { get; set; }            // optional (dùng cái nào cũng được)
            public int? TrangThaiId { get; set; }        // optional
            public string? TrangThaiText { get; set; }   // optional: "Có mặt" | "Đi trễ" | "Vắng" | "Vắng có phép"
            public string? Source { get; set; }          // "BLE" | "Manual"
        }


        private async Task PushStatusToFirebaseBoth(
        int diemDanhId,
        string mssv,
        string hoTenDayDu,        // ví dụ: "Nguyễn Văn A"
        int trangThaiId,          // Id trong bảng TrangThai
        string trangThaiText,     // "Có mặt" | "Đi trễ" | "Vắng" | "Vắng có phép"
        DateTime thoiGian,
        int? sinhVienId = null,   // Id SV trong SQL (để đẩy nhánh web cũ)
        double? latitude = null,
        double? longitude = null,
        string? deviceId = null,  // BLE DeviceId nếu có
        string? source = null     // "BLE" | "Manual" | "QR"
        )
        {
            var fb = new FirebaseClient("https://bluenet-e6525-default-rtdb.firebaseio.com");
            string sessionKey = diemDanhId.ToString(CultureInfo.InvariantCulture);

            // Tách "Họ và tên đệm" / "Tên" để nhánh web cũ vẫn chuẩn cấu trúc
            var (hoVaTenDem, ten) = SplitName(hoTenDayDu);

            // =========================
            // 1) Nhánh WEB cũ: "attendance_sessions/{diemDanhId}/{sinhVienId}"
            //    Web razor đang đọc: id, mssv, hoVaTenDem, ten, trangThai (int), thoiGian (ISO)
            // =========================
            if (sinhVienId.HasValue)
            {
                var webDoc = new
                {
                    id = sinhVienId.Value,
                    mssv = mssv,
                    hoVaTenDem = hoVaTenDem,
                    ten = ten,
                    trangThai = trangThaiId,
                    thoiGian = thoiGian.ToString("s", CultureInfo.InvariantCulture),
                    latitude,
                    longitude,
                    deviceId,
                    source = source ?? "Manual"
                };

                await fb
                    .Child("attendance_sessions")
                    .Child(sessionKey)
                    .Child(sinhVienId.Value.ToString(CultureInfo.InvariantCulture))
                    .PutAsync(webDoc);
            }

            // =========================
            // 2) Nhánh APP mới: "attendancesessions/{diemDanhId}/students/{MSSV}"
            //    App Flutter đang đọc: studentId, studentName, status (text), statusId (int),
            //    timecheckedin (HH:mm hoặc "Chưa điểm danh"), bluetoothID
            // =========================
            var appDoc = new
            {
                studentId = mssv,
                studentName = hoTenDayDu,
                status = trangThaiText,
                statusId = trangThaiId,
                timecheckedin = (trangThaiText == "Có mặt" || trangThaiText == "Đi trễ")
                    ? thoiGian.ToString("HH:mm")
                    : "Chưa điểm danh",
                bluetoothID = deviceId,
                latitude,
                longitude,
                deviceId,
                source = source ?? "Manual",
                updatedAt = thoiGian.ToString("s", CultureInfo.InvariantCulture)
            };

            // Patch để không ghi đè các field khác (ví dụ đã seed sẵn)
            await fb
                .Child("attendancesessions")
                .Child(sessionKey)
                .Child("students")
                .Child(mssv)
                .PatchAsync(appDoc);

            // Optional: cập nhật metadata buổi (cho tiện debug/hiển thị)
            await fb
                .Child("attendancesessions")
                .Child(sessionKey)
                .PatchAsync(new
                {
                    lastUpdateAt = thoiGian.ToString("s", CultureInfo.InvariantCulture),
                    lastUpdateSource = source ?? "Manual"
                });
        }

        private static (string hoVaTenDem, string ten) SplitName(string full)
        {
            if (string.IsNullOrWhiteSpace(full)) return ("", "");
            var parts = full.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return ("", parts[0]);
            var ten = parts[^1];
            var hoVaTenDem = string.Join(' ', parts[..^1]);
            return (hoVaTenDem, ten);
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpPost("lophocphan/{maLopHocPhan}/guithongbao")]
        public async Task<IActionResult> GuiThongBaoLopHocPhan(
        string maLopHocPhan,
        [FromBody] GuiThongBaoLopRequest model)
        {
            var senderUserId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(senderUserId))
                return Unauthorized("Không xác định được user gửi.");

            // Lấy tất cả sinh viên của lớp học phần
            var chiTietList = await _context.ChiTietLopHocPhans
                .Include(ct => ct.SinhVien)
                .Include(ct => ct.LopHocPhan)
                    .ThenInclude(ct => ct.MonHoc)
                .Where(ct => ct.LopHocPhan.MaLopHocPhan == maLopHocPhan)
                .ToListAsync();

            if (chiTietList == null || !chiTietList.Any())
                return NotFound("Không tìm thấy lớp học phần hoặc chưa có sinh viên.");

            // Lấy UserId của sinh viên
            var svUserIds = chiTietList
                .Where(ct => ct.SinhVien != null && !string.IsNullOrEmpty(ct.SinhVien.UserId))
                .Select(ct => ct.SinhVien.UserId)
                .Distinct()
                .ToList();

            if (svUserIds.Count == 0)
                return BadRequest("Lớp này chưa có sinh viên hoặc thông tin UserId chưa đủ.");

            // Insert ThongBao cho từng sinh viên
            var now = DateTime.Now;
            var thongBaos = svUserIds.Select(uid => new ThongBao
            {
                Title = model.Title,
                Content = model.Content,
                Time = now,
                ReceiverUserId = uid,
                SenderUserId = senderUserId,
                Type = model.Type ?? "LopHocPhan"
            }).ToList();

            await _context.ThongBaos.AddRangeAsync(thongBaos);
            await _context.SaveChangesAsync();

            var tenLop = chiTietList.FirstOrDefault()?.LopHocPhan?.MonHoc?.TenMonHoc ?? "Lớp học phần";


            foreach (var tb in thongBaos)
            {
                await PushNotificationToFirebase(tb.ReceiverUserId, tb, maLopHocPhan);
                var user = await _context.Users.FindAsync(tb.ReceiverUserId);

                if (!string.IsNullOrEmpty(user?.FcmToken))
                {
                    var fcmTitle = $"{tenLop} - Thông báo mới";
                    var fcmBody = tb.Title; 
                    await SendFcmPush(user.FcmToken, fcmTitle, fcmBody);
                }
            }

            return Ok(new { result = true, message = $"Đã gửi thông báo cho {svUserIds.Count} sinh viên trong lớp!" });
        }

        public class GuiThongBaoLopRequest
        {
            public string Title { get; set; }
            public string Content { get; set; }
            public string? Type { get; set; }
        }


        private async Task PushNotificationToFirebase(string receiverUserId, ThongBao tb, string maLopHocPhan)
        {
            var firebaseClient = new Firebase.Database.FirebaseClient("https://bluenet-e6525-default-rtdb.firebaseio.com");
            var giangVien = await _context.GiangViens.FirstOrDefaultAsync(gv => gv.UserId == tb.SenderUserId);
            var senderName = giangVien != null ? $"{giangVien.HoVaTenDem} {giangVien.Ten}" : "Hệ thống";

            var lopInfo = await _context.ChiTietLopHocPhans
                .Where(ct => ct.SinhVien.UserId == receiverUserId
                          && ct.LopHocPhan.MaLopHocPhan == maLopHocPhan)
                .Select(ct => new
                {
                    ct.LopHocPhan.MaLopHocPhan,
                    TenMonHoc = ct.LopHocPhan.MonHoc.TenMonHoc
                })
                .AsNoTracking()
                .SingleOrDefaultAsync();

            var tenMonHoc = lopInfo?.TenMonHoc ?? "Không rõ tên môn học";
            var maLhp = lopInfo?.MaLopHocPhan; 


            var data = new
            {
                id = tb.Id, // Id của thông báo trong SQL nếu có
                title = tb.Title,
                content = tb.Content,
                time = tb.Time.ToString("s"),
                type = tb.Type,
                senderUserId = tb.SenderUserId,
                senderName = senderName,
                tenMonHoc = tenMonHoc,
                maLopHocPhan = maLhp
            };

            // Push lên nhánh notification riêng cho từng user
            await firebaseClient
                .Child("notifications")
                .Child(receiverUserId)
                .PostAsync(data); // dùng PostAsync để tạo node mới (giữ lại nhiều thông báo)
        }


        private async Task SendFcmPush(string fcmToken, string title, string body)
        {
            await FcmService.SendNotificationAsync(fcmToken, title, body);
        }



        public class UpdateStatusRequest
        {
            public int DiemDanhId { get; set; }
            public int SinhVienId { get; set; }
            public int TrangThaiId { get; set; }
        }

        // POST: api/buoidiemdanh/capnhat-trangthai
        [HttpPost("buoidiemdanh/capnhat-trangthai")]
        public async Task<IActionResult> UpdateStudentStatus([FromBody] UpdateStatusRequest req)
        {
            if (req == null)
                return BadRequest("Invalid request");

            // tìm chi tiết điểm danh theo buổi + sinh viên
            var chitiet = await _context.ChiTietDiemDanhs
                .FirstOrDefaultAsync(x =>
                    x.DiemDanhId == req.DiemDanhId &&
                    x.SinhVienId == req.SinhVienId);

            if (chitiet == null)
                return NotFound("Không tìm thấy bản ghi điểm danh");

            chitiet.TrangThaiId = req.TrangThaiId;
            chitiet.ThoiGian = DateTime.Now;
            chitiet.GhiChu = "Giảng viên điểm danh";
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [AllowAnonymous]
        [HttpPost("buoidiemdanh/{id}/capnhattrangthaihetthoigian")]
        public async Task<IActionResult> CapNhatTrangThaiKhiHetThoiGian(int id)
        {
            var buoi = await _context.DiemDanhs
                .Include(b => b.LopHocPhan)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (buoi == null)
                return NotFound(new { result = false, message = "Không tìm thấy buổi điểm danh" });

            // Cập nhật trạng thái thành "Đã kết thúc"
            var trangThaiDaKetThucId = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "DiemDanh#" && t.TenTrangThai == "Đã đóng")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (trangThaiDaKetThucId == 0)
                return BadRequest(new { result = false, message = "Thiếu cấu hình trạng thái 'Đã kết thúc'" });

            buoi.TrangThaiId = trangThaiDaKetThucId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                result = true,
                message = "Đã cập nhật trạng thái buổi điểm danh thành 'Đã đóng' "
            });
        }


    }




}
