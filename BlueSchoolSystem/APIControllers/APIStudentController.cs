using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api/")]
    [ApiController]
    public class APIStudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public APIStudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
                        .Include(tt => tt.TrangThai)
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

        //Lấy danh sách sinh viên dựa vào mã lớp
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("laysinhvientheomalop/{maLop}")]
        public async Task<IActionResult> GetStudentsByClassCode(string maLop)
        {
            var students = await _context.SinhViens
                .Include(sv => sv.Lop)
                .Include(tt => tt.TrangThai)
                .Where(sv => sv.Lop.MaLop == maLop)
                .ToListAsync();
            if (students == null || !students.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy sinh viên nào trong lớp này"
                });
            }
            var count = students.Count;
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy dữ liệu thành công",
                soluongsinhvien = count,
                data = students
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("timkiemsinhvien")]
        public async Task<IActionResult> FilterStudents(string? keyword, string? maLop, bool? gioiTinh, string? maKhoa, string? nienKhoa)
        {
            var query = _context.SinhViens
                .Include(sv => sv.Lop)
                    .ThenInclude(l => l.Nganh)
                        .ThenInclude(n => n.Khoa)
                .Include(sv => sv.TrangThai)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(sv =>
                    sv.MSSV.Contains(keyword) ||
                    sv.HoVaTenDem.Contains(keyword) ||
                    sv.Ten.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(maKhoa))
            {
                string maKhoaLower = maKhoa.ToLower();
                query = query.Where(sv =>
                    sv.Lop != null &&
                    sv.Lop.Nganh != null &&
                    sv.Lop.Nganh.Khoa != null &&
                    sv.Lop.Nganh.Khoa.MaKhoa.ToLower() == maKhoaLower
                );
            }


            if (!string.IsNullOrWhiteSpace(maLop))
            {
                if (maLop == "__null__")
                {
                    query = query.Where(sv => sv.LopId == null);
                }
                else
                {
                    var arrMaLop = maLop
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList();

                    query = query.Where(sv => sv.Lop != null && arrMaLop.Contains(sv.Lop.MaLop));
                }
            }

            if (!string.IsNullOrWhiteSpace(nienKhoa) && int.TryParse(nienKhoa, out int year))
            {
                query = query.Where(sv => sv.NgayNhapHoc.Year == year);
            }

            if (gioiTinh.HasValue)
            {
                query = query.Where(sv => sv.GioiTinh == gioiTinh.Value);
            }

            var students = await query.ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lọc sinh viên thành công",
                soluongsinhvien = students.Count,
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
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
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

            //Nếu LopId = 0 thì tự động xếp lớp
            if (request.Student.LopId == 0)
            {
                // Tìm lớp còn trống đúng ngành (ưu tiên ít người nhất)

                var lopTrong = _context.LopHocs
                    .Where(l => l.Nganh.MaNganh == request.NganhHocId)
                    .Select(l => new
                    {
                        Lop = l,
                        SoLuongHienTai = _context.SinhViens.Count(sv => sv.LopId == l.Id)
                    })
                    .Where(x => x.SoLuongHienTai < 50) // Sĩ số tối đa = 50
                    .FirstOrDefault();

                if (lopTrong == null)
                {
                    return BadRequest("Không còn lớp nào trống thuộc ngành này! Vui lòng tạo lớp mới.");
                }

                // Gán vào lớp tìm được
                student.LopId = lopTrong.Lop.Id;

            }
            else if (request.Student.LopId == null)
            {
                student.LopId = null;
            }
            else
            {
                // Check lớp tự chọn có đủ chỗ không
                var lop = await _context.LopHocs.FindAsync(request.Student.LopId);
                if (lop == null)
                    return BadRequest("Lớp không tồn tại!");

                var soLuong = _context.SinhViens.Count(sv => sv.LopId == lop.Id);
                if (soLuong >= 50)
                    return BadRequest("Lớp đã đủ sĩ số!");

                student.LopId = request.Student.LopId;
            }
            // 3. Lưu vào DB
            _context.SinhViens.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Tạo sinh viên và tài khoản thành công!",
                studentId = student.Id,
                userId = user.Id
            });
        }

        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("suathongtinsinhvien/{mssv}")]
        public async Task<IActionResult> UpdateStudentByMSSV(string mssv, [FromBody] UpdateStudentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var student = await _context.SinhViens
                .Include(sv => sv.User)
                .FirstOrDefaultAsync(sv => sv.MSSV == mssv);

            if (student == null)
                return NotFound(new { message = "Không tìm thấy sinh viên." });

            if (request.MSSV != null) student.MSSV = request.MSSV;
            if (request.HoVaTenDem != null) student.HoVaTenDem = request.HoVaTenDem;
            if (request.Ten != null) student.Ten = request.Ten;
            if (request.CCCD != null) student.CCCD = request.CCCD;
            if (request.NgaySinh.HasValue) student.NgaySinh = request.NgaySinh.Value;
            if (request.GioiTinh.HasValue) student.GioiTinh = request.GioiTinh.Value;
            if (request.DiaChi != null) student.DiaChi = request.DiaChi;
            //if (request.LopId.HasValue) student.LopId = request.LopId.Value;
            //if (request.NgayNhapHoc.HasValue) student.NgayNhapHoc = request.NgayNhapHoc.Value;
            //if (request.NgayTotNghiep.HasValue) student.NgayTotNghiep = request.NgayTotNghiep.Value;
            if (request.TrangThaiId.HasValue) student.TrangThaiId = request.TrangThaiId.Value;
            if (request.GhiChu != null) student.GhiChu = request.GhiChu;
            //if (request.AvatarUrl != null) student.AvatarUrl = request.AvatarUrl;

            student.UpdatedAt = DateTime.Now;

            if (student.User != null)
            {
                var userManager = HttpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
                bool userChanged = false;

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && student.User.PhoneNumber != request.PhoneNumber)
                {
                    student.User.PhoneNumber = request.PhoneNumber;
                    userChanged = true;
                }
                if (!string.IsNullOrWhiteSpace(request.Email) && student.User.Email != request.Email)
                {
                    var result = await userManager.SetEmailAsync(student.User, request.Email);
                    if (!result.Succeeded)
                        return BadRequest(new { message = "Không thể cập nhật Email tài khoản." });
                    userChanged = true;
                }
                if (userChanged)
                {
                    var updateResult = await userManager.UpdateAsync(student.User);
                    if (!updateResult.Succeeded)
                        return BadRequest(new { message = "Lỗi khi cập nhật tài khoản sinh viên." });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Cập nhật thông tin sinh viên thành công!"
            });
        }


        //Lấy danh sách thời khóa biểu theo mã số sinh viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("thoikhoabieusinhvien/{mssv}")]
        public async Task<IActionResult> GetThoiKhoaBieuByMSSV(string mssv)
        {
            // Lấy MSSV từ JWT claim
            var mssvFromToken = User.FindFirst("username")?.Value;

            if (mssvFromToken == null)
            {
                return Unauthorized(new
                {
                    result = false,
                    code = 401,
                    message = "Không lấy được MSSV từ token"
                });
            }

            if (mssvFromToken != mssv)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    result = false,
                    code = 403,
                    message = "Bạn không có quyền truy cập thời khóa biểu của sinh viên khác"
                });
            }
            var tkb = await (from dk in _context.ChiTietLopHocPhans
                             join lhp in _context.LopHocPhans on dk.LopHocPhanId equals lhp.Id
                             join sv in _context.SinhViens on dk.SinhVienId equals sv.Id
                             join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                             join gv in _context.GiangViens on lhp.GiangVienId equals gv.Id
                             join ph in _context.PhongHocs on lhp.PhongHocId equals ph.Id
                             where sv.MSSV == mssv
                             orderby lhp.Thu, lhp.GioBatDau
                             select new
                             {
                                 sv.MSSV,
                                 lhp.MaLopHocPhan,
                                 lhp.TenLopHocPhan,
                                 MaMonHoc = mh.MaMonHoc,
                                 TenMonHoc = mh.TenMonHoc,
                                 TenGiangVien = gv.HoVaTenDem + " " + gv.Ten,
                                 MaPhongHoc = ph.MaPhongHoc,
                                 lhp.Thu,
                                 lhp.GioBatDau,
                                 lhp.GioKetThuc,
                                 lhp.NgayBatDau,
                                 lhp.NgayKetThuc
                             }).ToListAsync();

            if (!tkb.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy thời khóa biểu cho MSSV này"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy thời khóa biểu thành công",
                soluong = tkb.Count,
                data = tkb
            });
        }

        // Lấy danh sách lịch thi theo MSSV
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("lichthisinhvien/{mssv}")]
        public async Task<IActionResult> GetLichThiByMSSV(string mssv, [FromQuery] int? hocKyId)
        {
            // MSSV từ token
            var mssvFromToken = User.FindFirst("username")?.Value;
            if (mssvFromToken == null)
            {
                return Unauthorized(new
                {
                    result = false,
                    code = 401,
                    message = "Không lấy được MSSV từ token"
                });
            }

            if (mssvFromToken != mssv)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    result = false,
                    code = 403,
                    message = "Bạn không có quyền truy cập lịch thi của sinh viên khác"
                });
            }

            // Truy vấn cơ bản
            var query = from dk in _context.ChiTietLopHocPhans
                        join lhp in _context.LopHocPhans on dk.LopHocPhanId equals lhp.Id
                        join sv in _context.SinhViens on dk.SinhVienId equals sv.Id
                        join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                        join hk in _context.HocKys on lhp.HocKyId equals hk.Id
                        join lt in _context.LichThis on lhp.Id equals lt.LopHocPhanId
                        join ph in _context.PhongHocs on lt.PhongHocId equals ph.Id
                        join tt in _context.TrangThais on lt.TrangThaiId equals tt.Id into trangThaiGroup
                        from tt in trangThaiGroup.DefaultIfEmpty()
                        where sv.MSSV == mssv
                        select new
                        {
                            sv.MSSV,
                            HocKyId = hk.Id,
                            hk.TenHocKy,
                            hk.NgayBatDau,
                            mh.MaMonHoc,
                            mh.TenMonHoc,
                            lhp.MaLopHocPhan,
                            lhp.TenLopHocPhan,
                            lt.NgayThi,
                            GioBatDauThi = lt.GioBatDau,
                            GioKetThucThi = lt.GioKetThuc,
                            ph.MaPhongHoc,
                            lt.HinhThucThi,
                            TinhTrangLichThi = tt.TenTrangThai
                        };

            // Nếu có hocKyId thì lọc thêm
            if (hocKyId.HasValue && hocKyId.Value > 0)
            {
                query = query.Where(x => x.HocKyId == hocKyId.Value);
            }

            var lichThi = await query
                .GroupBy(x => new { x.HocKyId, x.TenHocKy, x.NgayBatDau })
                .Select(g => new
                {
                    HocKyId = g.Key.HocKyId,
                    TenHocKy = g.Key.TenHocKy,
                    NgayBatDau = g.Key.NgayBatDau,
                    LichThis = g.OrderBy(x => x.NgayThi).ThenBy(x => x.GioBatDauThi).ToList()
                })
                .OrderByDescending(x => x.NgayBatDau)
                .ToListAsync();

            if (!lichThi.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lịch thi cho MSSV này"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy lịch thi thành công",
                soluong = lichThi.Count,
                data = lichThi
            });
        }


        //Hàm chuyển điểm 10 sang điểm 4
        private double ConvertToHe4(double diem10)
        {
            if (diem10 >= 8.5) return 4.0;
            if (diem10 >= 7.0) return 3.0;
            if (diem10 >= 5.5) return 2.0;
            if (diem10 >= 4.0) return 1.0;
            return 0.0;
        }
        //Lấy điểm của sinh viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("diemsinhvien/{mssv}")]
        public async Task<IActionResult> GetDiemByMSSV(string mssv, [FromQuery] int? hocKyId)
        {
            var mssvFromToken = User.FindFirst("username")?.Value;
            if (mssvFromToken == null)
            {
                return Unauthorized(new { result = false, code = 401, message = "Không lấy được MSSV từ token" });
            }
            if (mssvFromToken != mssv)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { result = false, code = 403, message = "Bạn không có quyền truy cập điểm của sinh viên khác" });
            }

            // ✅ Truy vấn điểm
            var query = from bd in _context.BangDiems
                        join sv in _context.SinhViens on bd.SinhVienId equals sv.Id
                        join lhp in _context.LopHocPhans on bd.LopHocPhanId equals lhp.Id
                        join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                        join hk in _context.HocKys on lhp.HocKyId equals hk.Id
                        where sv.MSSV == mssv
                        select new
                        {
                            sv.MSSV,
                            HocKyId = hk.Id,
                            hk.TenHocKy,
                            hk.NgayBatDau,
                            mh.MaMonHoc,
                            mh.TenMonHoc,
                            mh.SoTinChi,
                            DiemCuoiKy = bd.DiemCuoiKy,
                            DiemQuaTrinh = bd.DiemChuyenCan

                        };

            if (hocKyId.HasValue && hocKyId.Value > 0)
            {
                query = query.Where(x => x.HocKyId == hocKyId.Value);
            }

            var diem = await query
                .GroupBy(x => new { x.HocKyId, x.TenHocKy, x.NgayBatDau })
                .Select(g => new
                {
                    HocKyId = g.Key.HocKyId,
                    TenHocKy = g.Key.TenHocKy,
                    NgayBatDau = g.Key.NgayBatDau,
                    Diems = g.OrderBy(x => x.MaMonHoc).ToList()
                })
                .OrderBy(x => x.NgayBatDau)
                .ToListAsync();

            if (!diem.Any())
            {
                return NotFound(new { result = false, code = 404, message = "Không tìm thấy điểm cho MSSV này" });
            }

            // ✅ Tính toán tích lũy toàn bộ
            double tongDiemHe4TichLuy = 0;
            int tongTinChiTichLuy = 0;
            int tongTinChiDat = 0;
            var allDiem = diem.SelectMany(d => d.Diems).ToList();

            foreach (var d in allDiem)
            {
                if (d.DiemCuoiKy.HasValue)
                {
                    double diemHe4 = ConvertToHe4(d.DiemCuoiKy.Value);
                    tongTinChiTichLuy += d.SoTinChi;
                    tongDiemHe4TichLuy += diemHe4 * d.SoTinChi;
                    if (diemHe4 > 0) tongTinChiDat += d.SoTinChi;
                }
            }

            // ✅ Danh sách tích lũy theo từng học kỳ
            var tichLuyList = new List<object>();
            double tongDiemTichLuy = 0;
            int tongTinChiTichLuy2 = 0;
            int tongTinChiDat2 = 0;

            foreach (var hk in diem.OrderBy(d => d.HocKyId))
            {
                var diemHocKyList = hk.Diems.Where(d => d.DiemCuoiKy.HasValue).ToList();
                if (diemHocKyList.Any())
                {
                    var tongTinChiHK = diemHocKyList.Sum(d => d.SoTinChi);
                    var tongDiemHK = diemHocKyList.Sum(d => ConvertToHe4(d.DiemCuoiKy.Value) * d.SoTinChi);

                    var diemTBHocKy = tongTinChiHK > 0 ? (tongDiemHK / tongTinChiHK).ToString("0.00") : "0.00";

                    // cộng dồn
                    tongTinChiTichLuy2 += tongTinChiHK;
                    tongDiemTichLuy += tongDiemHK;
                    tongTinChiDat2 += diemHocKyList.Where(d => ConvertToHe4(d.DiemCuoiKy.Value) > 0).Sum(d => d.SoTinChi);

                    var diemTBTichLuy = tongTinChiTichLuy2 > 0 ? (tongDiemTichLuy / tongTinChiTichLuy2).ToString("0.00") : "0.00";

                    tichLuyList.Add(new
                    {
                        hk.TenHocKy,
                        DiemTBHocKy = diemTBHocKy,
                        DiemTBTichLuy = diemTBTichLuy,
                        TinChiDat = tongTinChiDat2,
                        TongTinChiTichLuy = tongTinChiTichLuy2
                    });
                }
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy điểm thành công",
                soluong = diem.Count,
                data = diem,
                // ✅ Bổ sung phần tổng hợp
                tongTinChiTichLuy,
                tongTinChiDat,
                DiemTBTichLuy = tongTinChiTichLuy > 0 ? (tongDiemHe4TichLuy / tongTinChiTichLuy).ToString("0.00") : "0.00",
                tichLuyList
            });
        }



    }
}