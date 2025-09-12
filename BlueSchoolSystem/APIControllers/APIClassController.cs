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

    }
}
