using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

        // Lấy chi tiết Khoa viện
        //-----------------------
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laychitietkhoa")]
        public IActionResult GetFacultyDetail(int id)
        {
            var facultyDetail = _context.Khoas
                .Where(k => k.Id == id)
                .Include(k => k.ChiTietKhoaViens)
                    .ThenInclude(ct => ct.TruongKhoa)
                .Include(k => k.ChiTietKhoaViens)
                    .ThenInclude(ct => ct.PhoKhoa)
                .Include(k => k.ChiTietKhoaViens)
                    .ThenInclude(ct => ct.TroLiKhoa)
                .Select(k => new
                {
                    k.Id,
                    k.MaKhoa,
                    k.TenKhoa,
                    SoLuongNganh = _context.NganhHocs.Count(n => n.KhoaId == k.Id),
                    SoLuongGiangVien = _context.GiangViens.Count(gv => gv.KhoaId == k.Id),

                    ChiTiet = k.ChiTietKhoaViens.Select(ct => new
                    {
                        ct.Id,
                        ct.TruongKhoaId,
                        TruongKhoa = ct.TruongKhoa != null
                            ? ct.TruongKhoa.HoVaTenDem + " " + ct.TruongKhoa.Ten
                            : null,

                        ct.PhoKhoaId,
                        PhoKhoa = ct.PhoKhoa != null
                            ? ct.PhoKhoa.HoVaTenDem + " " + ct.PhoKhoa.Ten
                            : null,

                        ct.TroLiKhoaId,
                        TroLiKhoa = ct.TroLiKhoa != null
                            ? ct.TroLiKhoa.HoVaTenDem + " " + ct.TroLiKhoa.Ten
                            : null
                    }).ToList()
                })
                .FirstOrDefault();
            if (facultyDetail == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy khoa với ID đã cho"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                data = facultyDetail
            });
        }

        // 1. LẤY DANH SÁCH CƠ SỞ

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachcoso")]
        public IActionResult GetCoSos(string? keyword, string? maCoSo, string? tenCoSo)
        {
            var query = _context.CoSos.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(cs =>
                    cs.MaCoSo.ToLower().Contains(kw) ||
                    cs.TenCoSo.ToLower().Contains(kw));
            }

            if (!string.IsNullOrEmpty(maCoSo))
            {
                var kw = maCoSo.Trim().ToLower();
                query = query.Where(cs => cs.MaCoSo.ToLower().Contains(kw));
            }

            if (!string.IsNullOrEmpty(tenCoSo))
            {
                var kw = tenCoSo.Trim().ToLower();
                query = query.Where(cs => cs.TenCoSo.ToLower().Contains(kw));
            }

            var list = query.OrderBy(x => x.Id).ToList();

            return Ok(new
            {
                result = true,
                code = 200,
                message = list.Any()
                    ? "Lấy danh sách cơ sở thành công"
                    : "Không tìm thấy dữ liệu cơ sở phù hợp",
                soluong = list.Count,
                data = list
            });
        }

        // 2. THÊM CƠ SỞ MỚI

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("taomoi-coso")]
        public IActionResult CreateFacility([FromBody] CoSo model)
        {
            if (model == null ||
                string.IsNullOrWhiteSpace(model.MaCoSo) ||
                string.IsNullOrWhiteSpace(model.TenCoSo))
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Vui lòng nhập đầy đủ Mã cơ sở và Tên cơ sở"
                });
            }

            if (_context.CoSos.Any(c => c.MaCoSo == model.MaCoSo))
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = $"Mã cơ sở '{model.MaCoSo}' đã tồn tại"
                });
            }

            var newCoSo = new CoSo
            {
                MaCoSo = model.MaCoSo.Trim().ToUpper(),
                TenCoSo = model.TenCoSo.Trim(),
                DiaChi = model.DiaChi?.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude
            };

            _context.CoSos.Add(newCoSo);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Thêm cơ sở mới thành công",
                data = newCoSo
            });
        }

        // 3. SỬA CƠ SỞ

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("capnhat-coso/{id}")]
        public IActionResult EditFacility(int id, [FromBody] CoSo model)
        {
            var coSo = _context.CoSos.Find(id);

            if (coSo == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy cơ sở cần sửa"
                });
            }

            if (_context.CoSos.Any(c => c.MaCoSo == model.MaCoSo && c.Id != id))
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Mã cơ sở mới bị trùng"
                });
            }

            coSo.MaCoSo = model.MaCoSo.Trim().ToUpper();
            coSo.TenCoSo = model.TenCoSo.Trim();
            coSo.DiaChi = model.DiaChi?.Trim();
            coSo.Latitude = model.Latitude;
            coSo.Longitude = model.Longitude;

            _context.CoSos.Update(coSo);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Cập nhật cơ sở thành công",
                data = coSo
            });
        }
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("xoa-coso/{id}")]
        public IActionResult DeleteFacility(int id)
        {
            var coSo = _context.CoSos.Find(id);

            if (coSo == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy cơ sở cần xóa"
                });
            }

            var hasPhongHoc = _context.PhongHocs.Any(p => p.CoSoId == id);

            if (hasPhongHoc)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = $"Không thể xóa! '{coSo.TenCoSo}' đang có phòng học. Vui lòng xóa hoặc chuyển phòng học sang cơ sở khác trước."
                });
            }

            _context.CoSos.Remove(coSo);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Xóa cơ sở thành công"
            });
        }


        // 5. LẤY CHI TIẾT CƠ SỞ
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laychitietcoso")]
        public IActionResult GetFacilityDetail(int id)
        {
            var data = _context.CoSos
                .Where(c => c.Id == id)
                .Include(c => c.PhongHocs)
                .Select(c => new
                {
                    c.Id,
                    c.MaCoSo,
                    c.TenCoSo,
                    c.DiaChi,
                    c.Latitude,
                    c.Longitude,

                    SoLuongPhongHoc = c.PhongHocs.Count,

                    PhongHocs = c.PhongHocs.Select(p => new
                    {
                        p.Id,
                        p.MaPhongHoc,
                        p.TenPhongHoc,
                        p.SoChoNgoi
                    }).ToList()
                })
                .FirstOrDefault();

            if (data == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy cơ sở với ID đã cho"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                data = data
            });
        }
        [HttpGet("laydanhsachphonghoc")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRoomsByFacility(int coSoId, [FromQuery] string? keyword)
        {
            var query = _context.PhongHocs.Where(p => p.CoSoId == coSoId).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(p => p.MaPhongHoc.ToLower().Contains(keyword)
                                      || p.TenPhongHoc.ToLower().Contains(keyword));
            }

            var rooms = await query.Select(p => new
            {
                p.Id,
                p.MaPhongHoc,
                p.TenPhongHoc,
                p.SoChoNgoi
            }).ToListAsync();

            return Ok(new { result = true, data = rooms });
        }
        [HttpPost("taomoi-phonghoc")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateRoom([FromBody] PhongHoc model)
        {
            if (model == null || string.IsNullOrEmpty(model.MaPhongHoc) || string.IsNullOrEmpty(model.TenPhongHoc) || model.SoChoNgoi <= 0)
                return BadRequest(new { result = false, message = "Dữ liệu phòng học không hợp lệ." });

            var exists = await _context.PhongHocs.AnyAsync(p => p.CoSoId == model.CoSoId && p.MaPhongHoc == model.MaPhongHoc);
            if (exists)
                return BadRequest(new { result = false, message = $"Mã phòng {model.MaPhongHoc} đã tồn tại trong cơ sở." });

            _context.PhongHocs.Add(model);
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { result = true, message = $"Đã thêm phòng {model.TenPhongHoc} thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { result = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("suaphonghoc/{id}")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> EditRoom(int id, [FromBody] PhongHoc model)
        {
            var phongHoc = await _context.PhongHocs.FindAsync(id);
            if (phongHoc == null)
                return NotFound(new { result = false, message = "Không tìm thấy phòng học." });

            if (model == null || string.IsNullOrEmpty(model.MaPhongHoc) || string.IsNullOrEmpty(model.TenPhongHoc) || model.SoChoNgoi <= 0)
                return BadRequest(new { result = false, message = "Dữ liệu phòng học không hợp lệ." });

            var exists = await _context.PhongHocs.AnyAsync(p => p.CoSoId == model.CoSoId && p.MaPhongHoc == model.MaPhongHoc && p.Id != id);
            if (exists)
                return BadRequest(new { result = false, message = $"Mã phòng {model.MaPhongHoc} đã tồn tại trong cơ sở." });

            phongHoc.MaPhongHoc = model.MaPhongHoc;
            phongHoc.TenPhongHoc = model.TenPhongHoc;
            phongHoc.SoChoNgoi = model.SoChoNgoi;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { result = true, message = $"Cập nhật phòng {phongHoc.TenPhongHoc} thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { result = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpDelete("xoaphonghoc/{id}")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var phongHoc = await _context.PhongHocs.FindAsync(id);
            if (phongHoc == null)
                return NotFound(new { result = false, message = "Không tìm thấy phòng học." });

            // Kiểm tra ràng buộc (ví dụ: LichHoc)
            var inUse = await _context.LichHocs.AnyAsync(lh => lh.PhongHocId == id);
            if (inUse)
                return BadRequest(new { result = false, message = $"Phòng {phongHoc.MaPhongHoc} đang được sử dụng trong lịch học." });
            try
            {
                _context.PhongHocs.Remove(phongHoc);
                await _context.SaveChangesAsync();
                return Ok(new { result = true, message = $"Đã xóa phòng {phongHoc.MaPhongHoc} thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { result = false, message = "Lỗi hệ thống: " + ex.Message });
            }

        }


        [HttpGet("sukien")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAllSuKien()
        {
            var suKiens = await _context.SuKiens
           .OrderBy(x => x.NgayBatDau)
           .Select(x => new SuKienViewModel
           {
               Id = x.Id,
               TenSuKien = x.TenSuKien,
               MoTa = x.MoTa,
               NgayBatDau = x.NgayBatDau,
               NgayKetThuc = x.NgayKetThuc,
               DiaDiem = x.DiaDiem
           })
           .ToListAsync();

            return Ok(suKiens);
        }

        //Lấy sự kiện theo khoa
        [HttpGet("sukienkhoa/{maKhoa}")]
        public async Task<IActionResult> GetSuKienTheoMaKhoa(string maKhoa)
        {
            var suKiens = await _context.SuKiens
            .Where(x => x.Khoa != null && x.Khoa.MaKhoa == maKhoa)
            .OrderBy(x => x.NgayBatDau)
            .Select(x => new SuKienViewModel
            {
                Id = x.Id,
                TenSuKien = x.TenSuKien,
                MoTa = x.MoTa,
                NgayBatDau = x.NgayBatDau,
                NgayKetThuc = x.NgayKetThuc,
                DiaDiem = x.DiaDiem
            })
            .ToListAsync();

            return Ok(suKiens);
        }

    }
}
