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

        //Thêm giảng viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("themgiangvien")]
        public async Task<IActionResult> AddGiangVien([FromBody] CreateGiangVienWithUserRequest request)
        {
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
    }
}
