using BlueSchoolSystem.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APIFacultyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public APIFacultyController(ApplicationDbContext context)
        {
            _context = context;
        }
        // Lấy danh sách tất cả các khoa tại trường
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachkhoa")]
        public IActionResult GetFaculties(string? keyword, string? maKhoa, string? tenKhoa)
        {
            var query = _context.Khoas.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(k => k.MaKhoa.ToLower().Contains(keyword)
                                      || k.TenKhoa.ToLower().Contains(keyword));
            }

            if (!string.IsNullOrEmpty(maKhoa))
            {
                maKhoa = maKhoa.Trim().ToLower();
                query = query.Where(k => k.MaKhoa.ToLower().Contains(maKhoa));
            }

            if (!string.IsNullOrEmpty(tenKhoa))
            {
                tenKhoa = tenKhoa.Trim().ToLower();
                query = query.Where(k => k.TenKhoa.ToLower().Contains(tenKhoa));
            }

            if (!string.IsNullOrEmpty(tenKhoa))
            {
                tenKhoa = tenKhoa.Trim().ToLower();
                query = query.Where(k => k.TenKhoa.ToLower().Contains(tenKhoa));
            }
          
            var faculties = query.OrderBy(k => k.Id).ToList();
            var total = faculties.Count;
            
            if (!faculties.Any())
            {
                return Ok(new
                {
                    result = true,
                    code = 200,
                    message = "Không tìm thấy dữ liệu phù hợp",
                    soluong = 0,
                    data = new List<Khoa>()
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                soluong = total, 
                data = faculties
            });


        }
       
        // 2. THÊM MỚI KHOA
        [Authorize(Roles = "Admin,C", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("taomoi-khoa")]
        public IActionResult CreateFaculty([FromBody] Khoa model)
        {
            if (model == null || string.IsNullOrEmpty(model.MaKhoa) || string.IsNullOrEmpty(model.TenKhoa))
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Vui lòng nhập đầy đủ Mã khoa và Tên khoa"
                });
            }

            // Kiểm tra trùng Mã Khoa
            var checkExist = _context.Khoas.Any(k => k.MaKhoa == model.MaKhoa);
            if (checkExist)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = $"Mã khoa '{model.MaKhoa}' đã tồn tại trong hệ thống"
                });
            }

            // Tạo mới
            var newKhoa = new Khoa
            {
                MaKhoa = model.MaKhoa.ToUpper(), 
                TenKhoa = model.TenKhoa
            };

            _context.Khoas.Add(newKhoa);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Thêm khoa mới thành công",
                data = newKhoa
            });
        }

        // 3. CẬP NHẬT (SỬA) KHOA
        [Authorize(Roles = "Admin,CanBo", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("capnhat-khoa/{id}")]
        public IActionResult UpdateFaculty(int id, [FromBody] Khoa model)
        {
            var khoa = _context.Khoas.Find(id);

            if (khoa == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy khoa cần sửa"
                });
            }

            if (model.MaKhoa != khoa.MaKhoa)
            {
                var checkDuplicate = _context.Khoas.Any(k => k.MaKhoa == model.MaKhoa && k.Id != id);
                if (checkDuplicate)
                {
                    return BadRequest(new
                    {
                        result = false,
                        code = 400,
                        message = "Mã khoa mới bị trùng với khoa khác"
                    });
                }
            }

            khoa.MaKhoa = model.MaKhoa.ToUpper();
            khoa.TenKhoa = model.TenKhoa;

            _context.Khoas.Update(khoa);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Cập nhật thông tin thành công",
                data = khoa
            });
        }
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("xoa-khoa/{id}")]
        public IActionResult DeleteFaculty(int id)
        {
            var khoa = _context.Khoas.Find(id);

            if (khoa == null)
            {
                return Ok(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy khoa cần xóa"
                });
            }

            // --- KIỂM TRA RÀNG BUỘC: KHOA CÓ NGÀNH KHÔNG? ---
            var hasMajors = _context.NganhHocs.Any(n => n.KhoaId == id);

            if (hasMajors)
            {
                return Ok(new
                {
                    result = false,
                    code = 400, 
                    message = $"Không thể xóa!'{khoa.TenKhoa}' đang chứa ngành học. Vui lòng xóa hết các ngành thuộc khoa này trước."
                });
            }
            _context.Khoas.Remove(khoa);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Xóa khoa thành công"
            });
        }
    }
}
