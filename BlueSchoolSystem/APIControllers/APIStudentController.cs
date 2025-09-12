using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api/")]
    [ApiController]
    public class APIStudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public APIStudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //Lấy danh sách sinh viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachsinhvien")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _context.SinhViens
                        .Include(sv => sv.Lop)
                        .Include(tt => tt.TrangThai)
                        .ToListAsync();
            var tongsinhvien = await _context.SinhViens.CountAsync();
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                soluongsinhvien = tongsinhvien,
                data = students
            });
        }

        //Lấy danh sách sinh viên dựa vào mã lớp
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laysinhvientheomalop/{maLop}")]
        public async Task<IActionResult> GetStudentsByClassCode(string maLop)
        {
            var students = await _context.SinhViens
                .Include(sv => sv.Lop)
                .Include(tt => tt.TrangThai)
                .Where(sv => sv.Lop.MaLop == maLop)
                .ToListAsync();
            if (students == null || !students.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy sinh viên nào trong lớp này"
                });
            }
            var count = students.Count;
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                soluongsinhvien = count,
                data = students
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("timkiemsinhvien")]
        public async Task<IActionResult> FilterStudents(string? keyword, string? maLop, bool? gioiTinh, string? maKhoa)
        {
            var query = _context.SinhViens
                .Include(sv => sv.Lop)
                    .ThenInclude(n => n.Nganh)
                            .ThenInclude(k => k.Khoa)
                .Include(tt => tt.TrangThai)
                .AsQueryable();

            // Lọc theo keyword (tìm trong MSSV, Họ tên đệm, Tên)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(sv =>
                    sv.MSSV.Contains(keyword) ||
                    sv.HoVaTenDem.Contains(keyword) ||
                    sv.Ten.Contains(keyword));
            }

            //Lọc theo mã khoa
            if (!string.IsNullOrEmpty(maKhoa))
            {
                query = query.Where(sv =>
                    sv.Lop != null
                    && sv.Lop.Nganh != null
                    && sv.Lop.Nganh.Khoa != null
                    && sv.Lop.Nganh.Khoa.MaKhoa.ToLower() == maKhoa.ToLower()
                );
            }


            // Lọc theo mã lớp hoặc chưa xếp lớp
            if (!string.IsNullOrEmpty(maLop))
            {
                if (maLop == "__null__")
                {
                    query = query.Where(sv => sv.LopId == null);
                }
                else
                {
                    query = query.Where(sv => sv.Lop != null && sv.Lop.MaLop == maLop);
                }
            }

            // Lọc theo giới tính
            if (gioiTinh.HasValue)
            {
                query = query.Where(sv => sv.GioiTinh == gioiTinh.Value);
            }

            var students = await query.ToListAsync();
            var count = students.Count;

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lọc sinh viên thành công",
                soluongsinhvien = count,
                data = students
            });
        }


        //Thêm sinh viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("taomoisinhvien")]
        public async Task<IActionResult> CreateStudentWithUser([FromBody] CreateStudentWithUserRequest request)
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

            await _userManager.AddToRoleAsync(user, SD.Role_Student);
            
            // 2. Gán UserId vào student
            var student = request.Student;
            student.UserId = user.Id;
            student.CreatedAt = DateTime.Now;
            student.UpdatedAt = DateTime.Now;
            //if (request.Student.LopId == 0)
            //{
            //    student.LopId = request.Student.LopId;
            //}
            //else if (request.Student.LopId == null)
            //{
            //    student.LopId = null;
            //}
            //else
            //{
            //    student.LopId = request.Student.LopId;

            //}
            // 3. Lưu vào DB
            _context.SinhViens.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo sinh viên và tài khoản thành công!",
                studentId = student.Id,
                userId = user.Id
            });
        }

        //Sửa thông tin sinh viên
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("suathongtinsinhvien/{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = await _context.SinhViens.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Không tìm thấy sinh viên." });

            // Cập nhật thông tin sinh viên
            if (request.MSSV != null) student.MSSV = request.MSSV;
            if (request.HoVaTenDem != null) student.HoVaTenDem = request.HoVaTenDem;
            if (request.Ten != null) student.Ten = request.Ten;
            if (request.CCCD != null) student.CCCD = request.CCCD;
            if (request.NgaySinh.HasValue) student.NgaySinh = request.NgaySinh.Value;
            if (request.GioiTinh.HasValue) student.GioiTinh = request.GioiTinh.Value;
            if (request.DiaChi != null) student.DiaChi = request.DiaChi;
            if (request.LopId.HasValue) student.LopId = request.LopId.Value;
            if (request.NgayNhapHoc.HasValue) student.NgayNhapHoc = request.NgayNhapHoc.Value;
            if (request.NgayTotNghiep.HasValue) student.NgayTotNghiep = request.NgayTotNghiep.Value;
            if (request.TrangThai != null) student.TrangThai = request.TrangThai;
            if (request.GhiChu != null) student.GhiChu = request.GhiChu;
            if (request.AvatarUrl != null) student.AvatarUrl = request.AvatarUrl;

            student.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin sinh viên thành công!" });
        }

        //Lấy danh sách thời khóa biểu theo mã số sinh viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("thoikhoabieusinhvien/{mssv}")]
        public async Task<IActionResult> GetThoiKhoaBieuByMSSV(string mssv)
        {
            var tkb = await (from dk in _context.ChiTietLopHocPhans
                             join lhp in _context.LopHocPhans on dk.LopHocPhanId equals lhp.Id
                             join sv in _context.SinhViens on dk.SinhVienId equals sv.Id
                             join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                             join gv in _context.GiangViens on lhp.GiangVienId equals gv.Id
                             join ph in _context.PhongHocs on lhp.PhongHocId equals ph.Id
                             where sv.MSSV == mssv
                             orderby lhp.Thu, lhp.GioBatDau
                             select new
                             {
                                 sv.MSSV,
                                 lhp.MaLopHocPhan,
                                 lhp.TenLopHocPhan,
                                 MaMonHoc = mh.MaMonHoc,
                                 TenMonHoc = mh.TenMonHoc,
                                 TenGiangVien = gv.HoVaTenDem + " " + gv.Ten,
                                 MaPhongHoc = ph.MaPhongHoc,
                                 lhp.Thu,
                                 lhp.GioBatDau,
                                 lhp.GioKetThuc,
                                 lhp.NgayBatDau,
                                 lhp.NgayKetThuc
                             }).ToListAsync();

            if (!tkb.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy thời khóa biểu cho MSSV này"
                });
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