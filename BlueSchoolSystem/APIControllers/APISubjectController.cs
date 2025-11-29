using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api/")]
    [ApiController]
    public class APISubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public APISubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachmonhoc")]
        public IActionResult GetAllSubjects()
        {
            var data = _context.MonHocs.ToList();
            var total = data.Count;

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                tongmonhoc = total,
                data = data
            });
        }

        [HttpGet("monhoc/{id}")]
        public IActionResult GetSubjectById(int id)
        {
            var subject = _context.MonHocs.Find(id);

            if (subject == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy môn học",
                    data = (object?)null
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                data = subject
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("monhocchitiet/{id}")]
        public IActionResult GetSubjectDetail(int id)
        {
            var subject = _context.MonHocs
                .Include(m => m.NganhHocs)
                .Include(m => m.GiangVienMonHocs)
                    .ThenInclude(gv => gv.GiangVien)
                .FirstOrDefault(m => m.Id == id);

            if (subject == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy môn học",
                    data = (object?)null
                });
            }

            var result = new
            {
                Id = subject.Id,
                MaMonHoc = subject.MaMonHoc,
                TenMonHoc = subject.TenMonHoc,
                SoTinChi = subject.SoTinChi,
                MoTa = subject.MoTa,
                Nganhs = subject.NganhHocs.Select(n => new
                {
                    n.MaNganh,
                    n.TenNganh
                }),
                GiangViens = subject.GiangVienMonHocs.Select(gv => new
                {
                    gv.GiangVien.Id,
                    gv.GiangVien.MaGiangVien,
                    HoTen = $"{gv.GiangVien.HoVaTenDem} {gv.GiangVien.Ten}".Trim()
                })
            };

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                data = result
            });
        }


        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("taomonhoc")]
        public IActionResult CreateSubject([FromBody] MonHocCreate model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Dữ liệu không hợp lệ",
                    data = (object?)null
                });
            }

            var subject = new MonHoc
            {
                MaMonHoc = model.MaMonHoc,
                TenMonHoc = model.TenMonHoc,
                SoTinChi = model.SoTinChi,
                MoTa = model.MoTa
            };

            _context.MonHocs.Add(subject);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Thêm môn học thành công",
                data = subject
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("suamonhoc/{id}")]
        public IActionResult UpdateSubject(int id, [FromBody] MonHocUpdate model)
        {
            var subject = _context.MonHocs.Find(id);

            if (subject == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy môn học",
                    data = (object?)null
                });
            }

            subject.MaMonHoc = model.MaMonHoc;
            subject.TenMonHoc = model.TenMonHoc;
            subject.SoTinChi = model.SoTinChi;
            subject.MoTa = model.MoTa;

            _context.MonHocs.Update(subject);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Cập nhật môn học thành công",
                data = subject
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("xoamonhoc/{id}")]
        public IActionResult DeleteSubject(int id)
        {
            var subject = _context.MonHocs.Find(id);

            if (subject == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy môn học",
                    data = (object?)null
                });
            }

            bool isReferenced = _context.GiangVienMonHocs.Any(x => x.MonHocId == id);
            if (isReferenced)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Môn học đang được sử dụng, không thể xóa",
                    data = (object?)null
                });
            }

            _context.MonHocs.Remove(subject);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Xóa môn học thành công",
            });
        }

        //Gán môn học cho ngành
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("gannhommonghoc/{id}")]
        public IActionResult AssignNganhToSubject(int id, [FromBody] AssignNganhModel model)
        {
            var subject = _context.MonHocs
                .Include(m => m.NganhHocs)
                .FirstOrDefault(m => m.Id == id);

            if (subject == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy môn học"
                });
            }

            // Lấy danh sách ngành hợp lệ
            var nganhList = _context.NganhHocs
                .Where(n => model.NganhIds.Contains(n.Id))
                .ToList();

            // Reset lại danh sách
            subject.NganhHocs.Clear();

            // Thêm ngành mới
            foreach (var nganh in nganhList)
            {
                subject.NganhHocs.Add(nganh);
            }

            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Cập nhật ngành áp dụng thành công!"
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("gan-giang-vien-mon/{id}")]
        public IActionResult AssignGiangVien(int id, [FromBody] List<int> selectedGVs)
        {
            var subject = _context.MonHocs
                .Include(m => m.GiangVienMonHocs)
                .FirstOrDefault(m => m.Id == id);

            if (subject == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy môn học"
                });
            }

            // Xoá toàn bộ GV cũ
            subject.GiangVienMonHocs.Clear();

            // Gán lại danh sách GV mới
            foreach (var gvId in selectedGVs)
            {
                subject.GiangVienMonHocs.Add(new GiangVienMonHoc
                {
                    MonHocId = id,
                    GiangVienId = gvId
                });
            }

            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Gán giảng viên thành công"
            });
        }


    }
}
