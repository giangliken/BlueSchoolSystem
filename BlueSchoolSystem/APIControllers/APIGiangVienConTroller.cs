using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                             join ph in _context.PhongHocs on lhp.PhongHocId equals ph.Id into _ph
                             from ph in _ph.DefaultIfEmpty()
                             where lhp.GiangVienId == gv.Id
                             orderby lhp.Thu, lhp.GioBatDau
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
                                 lhp.PhongHocId,
                                 MaPhongHoc = ph != null ? ph.MaPhongHoc : null,
                                 TenPhongHoc = ph != null ? ph.TenPhongHoc : null,
                                 lhp.Thu,
                                 lhp.GioBatDau,
                                 lhp.GioKetThuc,
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

        // ✅ Lấy lớp phụ trách của giảng viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Teacher + "," + SD.Role_Admin)]
        [HttpGet("lopphutrach/{maGiangVien}")]
        public async Task<IActionResult> GetLopPhuTrachByMaGV(string maGiangVien)
        {
            // 🔹 1. Lấy mã giảng viên từ token
            var maGVFromToken = User.FindFirst("username")?.Value;
            if (maGVFromToken == null)
            {
                return Unauthorized(new
                {
                    result = false,
                    code = 401,
                    message = "Không lấy được mã giảng viên từ token"
                });
            }

            // 🔹 2. Nếu người gọi không phải admin và không phải chính giảng viên đó → chặn truy cập
            var isAdmin = User.IsInRole(SD.Role_Admin);
            if (!isAdmin && maGVFromToken != maGiangVien)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    result = false,
                    code = 403,
                    message = "Bạn không có quyền truy cập thông tin lớp phụ trách của giảng viên khác"
                });
            }

            // 🔹 3. Kiểm tra giảng viên có tồn tại không
            var gv = await _context.GiangViens.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.MaGiangVien == maGiangVien);
            if (gv == null)
                return NotFound(new { result = false, code = 404, message = "Không tìm thấy mã giảng viên" });

            // 🔹 4. Truy vấn lớp mà giảng viên này là chủ nhiệm
            var lopPhuTrach = await (from ctlh in _context.ChiTietLopHocs
                                     join lh in _context.LopHocs on ctlh.LopHocId equals lh.Id
                                     join nh in _context.NganhHocs on lh.NganhId equals nh.Id into _nh
                                     from nh in _nh.DefaultIfEmpty()
                                     join sv1 in _context.SinhViens on ctlh.LopTruongId equals sv1.Id into _sv1
                                     from sv1 in _sv1.DefaultIfEmpty()
                                     join sv2 in _context.SinhViens on ctlh.LopPhoId equals sv2.Id into _sv2
                                     from sv2 in _sv2.DefaultIfEmpty()
                                     join sv3 in _context.SinhViens on ctlh.BiThuId equals sv3.Id into _sv3
                                     from sv3 in _sv3.DefaultIfEmpty()
                                     where ctlh.GiangVienId == gv.Id
                                     select new
                                     {
                                         gv.MaGiangVien,
                                         GiangVien = gv.HoVaTenDem + " " + gv.Ten,
                                         lh.MaLop,
                                         lh.TenLop,
                                         NamNhapHoc = "20" + lh.MaLop.Substring(0, 2),
                                         NhomLop = lh.MaLop.Length > 5 ? lh.MaLop.Substring(5) : "",
                                         TenNganh = nh != null ? nh.TenNganh : "(Chưa cập nhật)",
                                         LopTruong = sv1 != null ? sv1.HoVaTenDem + " " + sv1.Ten : "(Chưa có)",
                                         LopPho = sv2 != null ? sv2.HoVaTenDem + " " + sv2.Ten : "(Chưa có)",
                                         BiThu = sv3 != null ? sv3.HoVaTenDem + " " + sv3.Ten : "(Chưa có)"
                                     }).ToListAsync();

            // 🔹 5. Kiểm tra dữ liệu trả về
            if (!lopPhuTrach.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Giảng viên này hiện chưa phụ trách lớp nào"
                });
            }

            // 🔹 6. Trả kết quả thành công
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy lớp phụ trách thành công",
                soluong = lopPhuTrach.Count,
                data = lopPhuTrach
            });
        }


    }
}
