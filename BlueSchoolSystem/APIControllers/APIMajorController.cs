using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        //Lấy chi tiết ngành học
        [HttpGet("laychitietnganhhoc/{id}")]
        public IActionResult GetMajorWithSubjects(int id)
        {
            var major = _context.NganhHocs
                .Include(n => n.Khoa)
                .Include(n => n.MonHocs)
                .FirstOrDefault(n => n.Id == id);

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
                data = major
            });
        }


        //Thêm ngành học
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("them-nganhhoc")]
        public IActionResult CreateMajor([FromBody] NganhHoc model)
        {
            if (model == null)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Dữ liệu ngành học không hợp lệ"
                });
            }

            // Kiểm tra xem khoa tồn tại
            var khoa = _context.Khoas.Find(model.KhoaId);
            if (khoa == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy khoa với ID cung cấp"
                });
            }

            _context.NganhHocs.Add(model);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Thêm ngành học thành công",
                data = model
            });
        }


        //Sửa ngành học
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("sua-nganhhoc/{id}")]
        public IActionResult UpdateMajor(int id, [FromBody] NganhHoc model)
        {
            var existing = _context.NganhHocs.Find(id);

            if (existing == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy ngành học cần sửa"
                });
            }

            // Kiểm tra khoa
            var khoa = _context.Khoas.Find(model.KhoaId);
            if (khoa == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy khoa với ID cung cấp"
                });
            }

            // Cập nhật dữ liệu
            existing.MaNganh = model.MaNganh;
            existing.TenNganh = model.TenNganh;
            existing.KhoaId = model.KhoaId;

            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Cập nhật ngành học thành công",
                data = existing
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("xoa-nganhhoc/{id}")]
        public IActionResult DeleteMajor(int id)
        {
            var major = _context.NganhHocs
                .Include(n => n.MonHocs)
                .FirstOrDefault(n => n.Id == id);

            if (major == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy ngành học cần xóa"
                });
            }

            // Xóa quan hệ với môn học
            if (major.MonHocs != null && major.MonHocs.Any())
            {
                major.MonHocs.Clear(); // EF Core sẽ xoá bản ghi trong bảng join
            }

            // Xóa ngành
            _context.NganhHocs.Remove(major);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Xóa ngành học thành công"
            });
        }



    }
}
