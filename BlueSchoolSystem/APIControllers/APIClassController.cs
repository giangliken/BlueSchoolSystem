using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APIClassController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public APIClassController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách tất cả các lớp học tại trường
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachlophoc")]
        public IActionResult GetClasses()
        {
            var classes = _context.LopHocs
                            .Include(lh => lh.Nganh)
                            .ThenInclude(lh => lh.Khoa)
                            .Select(lh => new
                            {
                                lh.Id,
                                lh.MaLop,
                                lh.TenLop,
                                lh.NganhId,
                                Nganh = lh.Nganh.TenNganh,
                                Khoa = lh.Nganh.Khoa.TenKhoa,
                                SiSo = _context.SinhViens.Count(sv => sv.Lop.MaLop == lh.MaLop)
                            })
        .ToList();

            if (classes == null || !classes.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy danh sách lớp học"
                });
            }
            var tongso = _context.LopHocs.Count();
            return Ok(new
            {
                result = true,
                code = 200,
                soluonglop = tongso,
                data = classes
            });
        }


        [HttpGet("laydanhsachlophoctheodieukien")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult GetClasses(string? maKhoa, string? maNganh, string? keyword, string? khoaHoc)
        {
            var query = _context.LopHocs
                .Include(lh => lh.Nganh)
                    .ThenInclude(n => n.Khoa)
                .AsQueryable();

            if (!string.IsNullOrEmpty(maKhoa))
            {
                query = query.Where(lh => lh.Nganh.Khoa.MaKhoa.ToLower() == maKhoa.ToLower());
            }

            if (!string.IsNullOrEmpty(maNganh))
            {
                query = query.Where(lh => lh.Nganh.MaNganh.ToLower() == maNganh.ToLower());
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(lh => lh.MaLop.Contains(keyword) || lh.TenLop.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(khoaHoc))
            {
                var prefix = khoaHoc.Substring(2, 2); // "2022" => "22"
                query = query.Where(l => l.MaLop.StartsWith(prefix));
            }


            var classes = query
                .Select(lh => new
                {
                    lh.Id,
                    lh.MaLop,
                    lh.TenLop,
                    lh.NganhId,
                    Nganh = lh.Nganh.TenNganh,
                    Khoa = lh.Nganh.Khoa.TenKhoa,
                    SiSo = _context.SinhViens.Count(sv => sv.LopId == lh.Id)
                })
                .ToList();

            return Ok(new
            {
                result = true,
                code = 200,
                soluonglop = classes.Count,
                data = classes
            });
        }


        //Lấy danh sách lớp học theo mã ngành
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachlophoctheonganh")]
        public IActionResult GetLop(string maNganh)
        {
            var data = (from l in _context.LopHocs
                        join n in _context.NganhHocs on l.NganhId equals n.Id
                        where n.MaNganh.ToLower() == maNganh.ToLower()
                        select new
                        {
                            id = l.Id,             
                            maLop = l.MaLop,        
                            tenLop = l.TenLop,
                            soLuongHienTai = _context.SinhViens.Count(sv => sv.LopId == l.Id),
                            siSoToiDa = 50,            
                        }).ToList();

            if (data.Count == 0)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lớp học nào cho mã ngành đã cho"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                tongsoluongloptheonganh = data.Count(),
                data = data
            });
        }


        // Lấy danh sách lớp học theo mã khoa
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laydanhsachlophoctheokhoa")]
        public IActionResult GetLopByKhoa(string maKhoa)
        {
            // Join LopHocs -> NganhHocs -> Khoas để filter đúng khoa
            var data = (from l in _context.LopHocs
                        join n in _context.NganhHocs on l.NganhId equals n.Id
                        join k in _context.Khoas on n.KhoaId equals k.Id
                        where k.MaKhoa.ToLower() == maKhoa.ToLower()
                        select new
                        {
                            id = l.Id,
                            maLop = l.MaLop,
                            tenLop = l.TenLop,
                            soLuongHienTai = _context.SinhViens.Count(sv => sv.LopId == l.Id),
                            siSoToiDa = 50 // hoặc l.SiSoToiDa nếu bạn có field này
                        }).ToList();

            if (data.Count == 0)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lớp học nào cho mã khoa đã cho"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                tongsoluongloptheokhoa = data.Count(),
                data = data
            });
        }


        // Lấy thông tin lớp học theo id
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laychitietlophoc")]
        public IActionResult GetClassDetail(int id)
        {
            var classDetail = _context.LopHocs
                .Where(lh => lh.Id == id)
                .Include(lh => lh.Nganh)
                    .ThenInclude(ng => ng.Khoa)
                .Include(lh => lh.ChiTietLopHocs)
                    .ThenInclude(ct => ct.GiangVien)
                .Include(lh => lh.ChiTietLopHocs)
                    .ThenInclude(ct => ct.LopTruong)
                .Include(lh => lh.ChiTietLopHocs)
                    .ThenInclude(ct => ct.LopPho)
                .Include(lh => lh.ChiTietLopHocs)
                    .ThenInclude(ct => ct.BiThu)
                .Select(lh => new
                {
                    lh.Id,
                    lh.MaLop,
                    lh.TenLop,
                    lh.MoTa,
                    lh.NganhId,
                    Nganh = lh.Nganh.TenNganh,
                    Khoa = lh.Nganh.Khoa.TenKhoa,
                    SiSo = _context.SinhViens.Count(sv => sv.LopId == lh.Id),
                    // Lấy info detail lớp học)
                    ChiTiet = lh.ChiTietLopHocs.Select(ct => new
                    {
                        ct.Id,
                        ct.GiangVienId,
                        TroLiHocTap = ct.GiangVien != null
                                            ? ct.GiangVien.HoVaTenDem + " " + ct.GiangVien.Ten
                                            : null,
                        ct.LopTruongId,
                        LopTruong = ct.LopTruong != null
                                            ? ct.LopTruong.HoVaTenDem + " " + ct.LopTruong.Ten
                                            : null,
                        ct.LopPhoId,
                        LopPho = ct.LopPho != null
                                            ? ct.LopPho.HoVaTenDem + " " + ct.LopPho.Ten
                                            : null,
                        ct.BiThuId,
                        BiThu = ct.BiThu != null
                                            ? ct.BiThu.HoVaTenDem + " " + ct.BiThu.Ten
                                            : null,
                    }).ToList()
                })
                .FirstOrDefault();

            if (classDetail == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lớp học với ID đã cho"
                });
            }
            return Ok(new
            {
                result = true,
                code = 200,
                data = classDetail
            });
        }

        // API thêm lớp học
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("themlophoc")]
        public IActionResult CreateClass([FromBody] CreateClassRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Dữ liệu gửi lên không hợp lệ"
                });
            }

            // Check ngành tồn tại
            var nganh = _context.NganhHocs.FirstOrDefault(n => n.Id == model.NganhId);
            if (nganh == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Ngành học không tồn tại"
                });
            }

            // Check trùng mã lớp
            var exists = _context.LopHocs.Any(l => l.MaLop.ToLower() == model.MaLop.ToLower());
            if (exists)
            {
                return Conflict(new
                {
                    result = false,
                    code = 409,
                    message = "Mã lớp đã tồn tại"
                });
            }

            // Tạo lớp học
            var lop = new LopHoc
            {
                MaLop = model.MaLop,
                TenLop = model.TenLop,
                MoTa = model.MoTa,
                NganhId = model.NganhId
            };

            _context.LopHocs.Add(lop);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Thêm lớp học thành công",
                data = new
                {
                    lop.Id,
                    lop.MaLop,
                    lop.TenLop,
                    lop.NganhId
                }
            });
        }


        [HttpPost("taoloptudong")]
        public IActionResult TaoLopTuDong(AutoClassRequest req)
        {
            // Validate đầu vào
            if (req.SoLuong < 1)
                return BadRequest(new { message = "Số lượng lớp phải >= 1" });

            // Lấy ngành
            var nganh = _context.NganhHocs
                .Include(n => n.Khoa)
                .FirstOrDefault(n => n.Id == req.NganhId);

            if (nganh == null)
                return NotFound(new { message = "Ngành không tồn tại" });

            string maKhoa = nganh.Khoa.MaKhoa;  // ví dụ: DTH

            // Lấy YY từ 2022 → 22
            string khoaShort = req.Khoa.ToString().Substring(2, 2);

            // Lấy mã lớp đã tồn tại để tránh trùng
            var existing = _context.LopHocs
                .Where(l => l.MaLop.StartsWith(khoaShort + maKhoa))
                .Select(l => l.MaLop)
                .ToList();

            var createdList = new List<string>();
            int index = 1;

            while (createdList.Count < req.SoLuong)
            {
                string maLop = GenerateClassCode(khoaShort, maKhoa, index);

                if (!existing.Contains(maLop))
                {
                    createdList.Add(maLop);

                    _context.LopHocs.Add(new LopHoc
                    {
                        MaLop = maLop,
                        TenLop = maLop,
                        NganhId = req.NganhId
                    });
                }
                index++;
            }

            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                data = createdList
            });
        }


        private static readonly char[] GroupLetters =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private string GenerateClassCode(string khoaShort, string maKhoa, int index)
        {
            // Mỗi chữ cái đại diện cho 2 lớp: A1, A2 → B1, B2 → ...
            int groupIndex = (index - 1) / 2;
            char groupLetter = GroupLetters[groupIndex];

            int number = ((index - 1) % 2) + 1; // 1 hoặc 2

            return $"{khoaShort}{maKhoa}{groupLetter}{number}";
        }


        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("xoalophoc")]
        public IActionResult DeleteClass(string maLop)
        {
            if (string.IsNullOrEmpty(maLop))
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Mã lớp không hợp lệ"
                });
            }

            var lop = _context.LopHocs.FirstOrDefault(l => l.MaLop.ToLower() == maLop.ToLower());
            if (lop == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lớp học"
                });
            }

            // Check lớp có sinh viên không
            var sinhVienCount = _context.SinhViens.Count(sv => sv.LopId == lop.Id);
            if (sinhVienCount > 0)
            {
                return Conflict(new
                {
                    result = false,
                    code = 409,
                    message = "Không thể xóa lớp học vì vẫn còn sinh viên thuộc lớp này"
                });
            }

            // Xóa chi tiết trước
            var chitiet = _context.ChiTietLopHocs.Where(ct => ct.LopHocId == lop.Id);
            _context.ChiTietLopHocs.RemoveRange(chitiet);

            // Xóa lớp
            _context.LopHocs.Remove(lop);
            _context.SaveChanges();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Xóa lớp học thành công"
            });
        }



    }
}
