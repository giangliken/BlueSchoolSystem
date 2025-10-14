using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

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


        // Lấy lịch giảng dạy của giảng viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("lichgiangday/{maGiangVien}")]
        public async Task<IActionResult> GetThoiKhoaBieuByMaGV(string maGiangVien)
        {
            var gv = await _context.GiangViens.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.MaGiangVien == maGiangVien);
            if (gv == null)
                return NotFound(new { result = false, code = 404, message = "Không tìm thấy mã giảng viên" });
            var tkb = await (from lhp in _context.LopHocPhans
                             join mh in _context.MonHocs on lhp.MonHocId equals mh.Id into _mh
                             from mh in _mh.DefaultIfEmpty()
                             where lhp.GiangVienId == gv.Id
                             //orderby lhp.Thu, lhp.GioBatDau
                             select new
                             {
                                 gv.MaGiangVien,
                                 HoTen = gv.HoVaTenDem + " " + gv.Ten,
                                 GiangVienId = gv.Id,
                                 LopHocPhanId = lhp.Id,
                                 lhp.MaLopHocPhan,
                                 lhp.TenLopHocPhan,
                                 lhp.MoTa,
                                 lhp.MonHocId,
                                 MaMonHoc = mh != null ? mh.MaMonHoc : null,
                                 TenMonHoc = mh != null ? mh.TenMonHoc : null,
                                 lhp.NgayBatDau,
                                 lhp.NgayKetThuc,
                                 lhp.SiSo,
                                 lhp.TrangThai
                             }).ToListAsync();

            if (!tkb.Any())
            {
                return NotFound(new { result = false, code = 404, message = "Không có thời khóa biểu" });
            }
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy thời khóa biểu thành công",
                soluong = tkb.Count,
                data = tkb
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

            var buoi = new DiemDanh
            {
                LopHocPhanId = lopHocPhanId,
                Ngay = model.Ngay,
                Code = code,
                CreatedAt = DateTime.Now,
                ExpireAt = model.ExpireAt ?? DateTime.Now.AddMinutes(20),
                GhiChu = model.GhiChu,
                TrangThaiId = 1 // Đang mở
            };
            _context.DiemDanhs.Add(buoi);
            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> UpdateTrangThaiDiemDanh([FromBody] UpdateTrangThaiModel model)
        {
            var chiTiet = await _context.ChiTietDiemDanhs
                .FirstOrDefaultAsync(x => x.DiemDanhId == model.DiemDanhId && x.SinhVienId == model.SinhVienId);
            if (chiTiet == null)
            {
                // Nếu chưa có thì tạo mới (trường hợp chỉnh cho sinh viên bị vắng)
                chiTiet = new ChiTietDiemDanh
                {
                    DiemDanhId = model.DiemDanhId,
                    SinhVienId = model.SinhVienId,
                    TrangThaiId = model.TrangThai,
                    ThoiGian = DateTime.Now
                };
                _context.ChiTietDiemDanhs.Add(chiTiet);
            }
            else
            {
                chiTiet.TrangThaiId = model.TrangThai;
                chiTiet.ThoiGian = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            return Ok(new { result = true });
        }
        public class UpdateTrangThaiModel
        {
            public int DiemDanhId { get; set; }
            public int SinhVienId { get; set; }
            public int TrangThai { get; set; }
        }


    }
}
