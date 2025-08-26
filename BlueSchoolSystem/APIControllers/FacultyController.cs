using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class FacultyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public FacultyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách tất cả các khoa tại trường
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachkhoa")]
        public IActionResult GetFaculties()
        {
            var faculties = _context.Khoas.ToList();
            if (faculties == null || !faculties.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy danh sách khoa học"
                });
            }
            var tongso = _context.Khoas.Count();
            return Ok(new
            {
                result = true,
                code = 200,
                soluongkhoa = tongso,
                data = faculties
            });
        }
    }
}
