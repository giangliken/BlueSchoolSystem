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
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("timkiemsinhvien")]
        public async Task<IActionResult> FilterStudents(string? keyword, string? maLop, bool? gioiTinh)
        {
            var query = _context.SinhViens
                .Include(sv => sv.Lop)
                .AsQueryable();

            // Lọc theo keyword (tên hoặc MSSV)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(sv =>
                    sv.MSSV.Contains(keyword) ||
                    sv.HoVaTenDem.Contains(keyword) ||
                    sv.Ten.Contains(keyword));
            }

            // Lọc theo mã lớp
            if (!string.IsNullOrEmpty(maLop))
            {
                query = query.Where(sv => sv.Lop.MaLop == maLop);
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
                Email = request.Email
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

    }
}
