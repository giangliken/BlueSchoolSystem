using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APIMajorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public APIMajorController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Lấy danh sách tất cả các ngành học tại trường
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachnganhhoc")]
        public IActionResult GetMajors()
        {
            var majors = _context.NganhHocs.ToList();

            if (majors == null || !majors.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy danh sách ngành học"
                });
            }
            var tongso = _context.NganhHocs.Count();
            return Ok(new
            {
                result = true,
                code = 200,
                soluongnganh = tongso,
                data = majors
            });
        }


        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachnganhtheomakhoa")]
        public IActionResult LayDSNganhTheoMaKhoa(string maKhoa)
        {
            var majors = _context.NganhHocs
                .Where(n => n.Khoa.MaKhoa.ToLower() == maKhoa.ToLower())
                .Select(n => new {
                    n.Id,
                    n.MaNganh,
                    n.TenNganh
                })
                .ToList();

            // Chỉ cần check count thôi!
            if (majors.Count == 0)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy ngành học với mã khoa đã cho",
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy thông tin thành công",
                tongsonganhthuockhoa = majors.Count,
                data = majors
            });
        }


        // Lấy thông tin ngành học theo id
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laythongtinnganhhoc")]
        public IActionResult GetMajorById(int id)
        {
            var major = _context.NganhHocs.Find(id);

            if (major == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy ngành học với ID đã cho"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy thông tin thành công",
                data = major
            });
        }

    }
}
