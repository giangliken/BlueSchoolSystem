using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public TestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("usernames")]
        public async Task<IActionResult> GetAllUsernames()
        {
            var usernames = await _context.Users
                .Select(u => u.UserName)
                .ToListAsync();
            var totalUsers = await _context.Users.CountAsync();
            return Ok(new
            {
                Usernames = usernames,
                Total = totalUsers
            });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddStudent([FromBody] SinhVien student)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Auto set ngày tạo
            student.CreatedAt = DateTime.Now;
            student.UpdatedAt = DateTime.Now;

            _context.SinhViens.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student added successfully!" });
        }

        

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst("userId")?.Value;

            if (userId == null)
                return Unauthorized();

            var user = await _userManager.Users
                .Include(u => u.SinhViens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User không tồn tại!" });

            return Ok(new
            {
                result = true,
                code = 200,
                user.Id,
                user.UserName,
                user.Email,
                //user.Student.MSSV,
                //ho_dem = $"{user.Student.HoVaTenDem}",
                //ten = $"{user.Student.Ten}",
                //user.Student.MaLop,
                //user.Student.MaKhoa,
                //user.Student.MaNganh,
                //user.Student.TrangThai
            });
        }


        [HttpGet]
        public IActionResult Ping()
        {
            return Ok(new { message = "Server is alive" });
        }

    }
}
