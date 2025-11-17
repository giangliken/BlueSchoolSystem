using Azure.Core;
using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Firebase.Database;
using Firebase.Database.Query;
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


        // Lấy danh sách thời khóa biểu theo mã số sinh viên
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("thoikhoabieusinhvien/{mssv}")]
        public async Task<IActionResult> GetThoiKhoaBieuByMSSV(string mssv)
        {
            // 🔹 Lấy MSSV từ JWT claim
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

            // 🔹 Lấy danh sách thời khóa biểu (join thêm PhongHoc)
            var tkbData = await (
                from ct in _context.ChiTietLopHocPhans
                join sv in _context.SinhViens on ct.SinhVienId equals sv.Id
                join lhp in _context.LopHocPhans on ct.LopHocPhanId equals lhp.Id
                join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                join gv in _context.GiangViens on lhp.GiangVienId equals gv.Id into gjv
                from gv in gjv.DefaultIfEmpty()
                join lh in _context.LichHocs on lhp.Id equals lh.LopHocPhanId
                join ph in _context.PhongHocs on lh.PhongHocId equals ph.Id into gph
                from ph in gph.DefaultIfEmpty() // Cho phép null nếu chưa có phòng học
                where sv.MSSV == mssv
                orderby lh.Ngay, lh.GioBatDau
                select new
                {
                    lhp.MaLopHocPhan,
                    //lhp.TenLopHocPhan,
                    MaMonHoc = mh.MaMonHoc,
                    TenMonHoc = mh.TenMonHoc,
                    TenGiangVien = gv != null ? (gv.HoVaTenDem + " " + gv.Ten) : "Chưa có giảng viên",
                    MaPhongHoc = ph != null ? ph.MaPhongHoc : "Chưa có phòng",
                    lh.Ngay,
                    lh.GioBatDau,
                    lh.GioKetThuc,
                    lhp.NgayBatDau,
                    lhp.NgayKetThuc
                }
            ).ToListAsync();

            if (!tkbData.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy thời khóa biểu cho MSSV này"
                });
            }

            // 🔹 Tính tiết bắt đầu và số tiết dựa vào giờ học
            var tkb = tkbData.Select(item =>
            {
                int ToTiet(TimeSpan gio)
                {
                    if (gio <= TimeSpan.Parse("6:45")) return 1;
                    if (gio <= TimeSpan.Parse("07:30")) return 2;
                    if (gio <= TimeSpan.Parse("08:15")) return 3;
                    if (gio <= TimeSpan.Parse("09:20")) return 4;
                    if (gio <= TimeSpan.Parse("10:05")) return 5;
                    if (gio <= TimeSpan.Parse("10:50")) return 6;
                    if (gio <= TimeSpan.Parse("12:30")) return 7;
                    if (gio <= TimeSpan.Parse("13:10")) return 8;
                    if (gio <= TimeSpan.Parse("14:00")) return 9;
                    if (gio <= TimeSpan.Parse("15:05")) return 10;
                    if (gio <= TimeSpan.Parse("15:50")) return 11;
                    if (gio <= TimeSpan.Parse("16:35")) return 12;
                    if (gio <= TimeSpan.Parse("18:00")) return 13;
                    if (gio <= TimeSpan.Parse("18:45")) return 14;
                    return 15;
                }

                int tietBatDau = ToTiet(item.GioBatDau);
                int tietKetThuc = ToTiet(item.GioKetThuc);
                int soTiet = tietKetThuc - tietBatDau ;

                return new ThoiKhoaBieuViewModel
                {
                    MaLopHocPhan = item.MaLopHocPhan,
                    //TenLopHocPhan = item.TenLopHocPhan,
                    MaMonHoc = item.MaMonHoc,
                    TenMonHoc = item.TenMonHoc,
                    TenGiangVien = item.TenGiangVien,
                    MaPhongHoc = item.MaPhongHoc,
                    NgayHoc = item.Ngay,
                    GioBatDau = item.GioBatDau.ToString(@"hh\:mm"),
                    GioKetThuc = item.GioKetThuc.ToString(@"hh\:mm"),
                    NgayBatDau = item.NgayBatDau,
                    NgayKetThuc = item.NgayKetThuc,
                    TietBatDau = tietBatDau,
                    SoTiet = soTiet
                };
            }).ToList();

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


        // ✅ Hàm chuyển đổi điểm 10 → thang 4
        private double ConvertToHe4(double diem10)
        {
            if (diem10 >= 8.5) return 4.0;
            if (diem10 >= 7.8) return 3.5;
            if (diem10 >= 7.0) return 3.0;
            if (diem10 >= 6.3) return 2.5;
            if (diem10 >= 5.5) return 2.0;
            if (diem10 >= 4.8) return 1.5;
            if (diem10 >= 4.0) return 1.0;
            if (diem10 >= 3.0) return 0.5;
            if (diem10 >= 0) return 0;
            return 0.0;
        }


        //  Hàm tính tổng hợp toàn bộ điểm
        private (double tongDiemHe4TichLuy, int tongTinChiTichLuy, int tongTinChiDat)
        TinhTongHop(IEnumerable<DiemMonHocViewModel> allDiem)
        {
            double tongDiemHe4TichLuy = 0;
            int tongTinChiTichLuy = 0;
            int tongTinChiDat = 0;

            foreach (var d in allDiem)
            {
                if (d.SoTinChi <= 0) continue;

                // Điểm 10 để chuyển sang thang 4


                double diem10;
                if (d.SoTinChi > 1)
                {
                    diem10 = d.DiemTongKet ?? 0;
                }
                else
                {
                    diem10 = d.DiemChuyenCan ?? 0;
                }

                double diemHe4 = diem10 >= 0 ? ConvertToHe4(diem10) : 0;

                //  Tích lũy: chỉ cộng nếu đạt (>= 1.0) và đủ điểm
                if (diemHe4 >= 1.0 &&
                    !((d.SoTinChi > 1 && (!d.DiemChuyenCan.HasValue || !d.DiemCuoiKy.HasValue)) ||
                      (d.SoTinChi == 1 && !d.DiemChuyenCan.HasValue)))
                {
                    tongTinChiTichLuy += d.SoTinChi;
                    tongDiemHe4TichLuy += diemHe4 * d.SoTinChi;
                    tongTinChiDat += d.SoTinChi;
                }
            }

            return (tongDiemHe4TichLuy, tongTinChiTichLuy, tongTinChiDat);
        }


        //  Hàm tính tích lũy theo từng học kỳ
        private List<TichLuyHocKyViewModel> TinhTichLuyTheoHocKy(List<DiemHocKyViewModel> diem)
        {
            var tichLuyList = new List<TichLuyHocKyViewModel>();
            double tongDiemTichLuy = 0;
            int tongTinChiTichLuy = 0;

            foreach (var hk in diem.OrderBy(d => d.HocKyId))
            {
                // ✅ Lấy tất cả môn để tính GPA học kỳ
                var diemHocKyList = hk.Diems
                    .Where(d => d.SoTinChi > 0)
                    .Select(d =>
                    {
                        double diem10;
                        if (d.SoTinChi > 1)
                        {
                            diem10 = d.DiemTongKet ?? 0; 
                        }
                        else
                        {
                            diem10 = d.DiemChuyenCan ?? 0;
                        }

                        return new
                        {
                            d.SoTinChi,
                            d.DiemChuyenCan,
                            d.DiemCuoiKy,
                            d.DiemTongKet,
                            DiemHe4 = ConvertToHe4(diem10),
                            DuDiem = (d.SoTinChi > 1 && d.DiemChuyenCan.HasValue && d.DiemCuoiKy.HasValue)
                                     || (d.SoTinChi == 1 && d.DiemChuyenCan.HasValue)
                        };
                    })
                    .ToList();

                if (!diemHocKyList.Any()) continue;

                // ✅ GPA học kỳ (tính tất cả, kể cả rớt, thiếu điểm coi là 0)
                var tongTinChiHocKy = diemHocKyList.Sum(d => d.SoTinChi);
                var tongDiemHocKy = diemHocKyList.Sum(d => d.DiemHe4 * d.SoTinChi);
                var diemTBHocKy = tongTinChiHocKy > 0 ? tongDiemHocKy / tongTinChiHocKy : 0.0;

                // ✅ Tích lũy: chỉ tính môn ĐẠT (>= 1.0) và có đủ điểm
                var monDat = diemHocKyList
                    .Where(d => d.DiemHe4 >= 1.0 && d.DuDiem)
                    .ToList();

                var tongTinChiHocKyDat = monDat.Sum(d => d.SoTinChi);
                var tongDiemHocKyDat = monDat.Sum(d => d.DiemHe4 * d.SoTinChi);

                tongTinChiTichLuy += tongTinChiHocKyDat;
                tongDiemTichLuy += tongDiemHocKyDat;

                var diemTBTichLuy = tongTinChiTichLuy > 0 ? tongDiemTichLuy / tongTinChiTichLuy : 0.0;

                tichLuyList.Add(new TichLuyHocKyViewModel
                {
                    TenHocKy = hk.TenHocKy,
                    DiemTBHocKy = Math.Round(diemTBHocKy, 2, MidpointRounding.AwayFromZero),     // GPA học kỳ (kể cả thiếu điểm = 0)
                    DiemTBTichLuy = Math.Round(diemTBTichLuy, 2, MidpointRounding.AwayFromZero), // GPA tích lũy (chỉ môn đạt)
                    TinChiDat = tongTinChiHocKyDat,
                    TongTinChiTichLuy = tongTinChiTichLuy
                });
            }

            return tichLuyList;
        }


        ////Lấy thông tin họ và tên của sinh viên khi biết mssv
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        //[HttpGet("layhovatensinhvien/{mssv}")]



        //  Lấy điểm của sinh viên
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
                            HocKyId = hk.Id,
                            hk.TenHocKy,
                            hk.NgayBatDau,
                            mh.MaMonHoc,
                            mh.TenMonHoc,
                            mh.SoTinChi,
                            DiemChuyenCan = bd.DiemChuyenCan,
                            DiemCuoiKy = bd.DiemCuoiKy

                        };

            if (hocKyId.HasValue && hocKyId.Value > 0)
            {
                query = query.Where(x => x.HocKyId == hocKyId.Value);
            }

            var diem = await query
                .GroupBy(x => new { x.HocKyId, x.TenHocKy, x.NgayBatDau })
                .Select(g => new DiemHocKyViewModel
                {
                    HocKyId = g.Key.HocKyId,
                    TenHocKy = g.Key.TenHocKy,
                    NgayBatDau = g.Key.NgayBatDau,
                    Diems = g.OrderBy(x => x.MaMonHoc)
                             .Select(x => new DiemMonHocViewModel
                             {
                                 MaMonHoc = x.MaMonHoc,
                                 TenMonHoc = x.TenMonHoc,
                                 SoTinChi = x.SoTinChi,
                                 DiemChuyenCan = x.DiemChuyenCan,
                                 DiemCuoiKy = x.DiemCuoiKy

                             }).ToList()
                })
                .OrderByDescending(x => x.NgayBatDau)
                .ToListAsync();

            if (!diem.Any())
            {
                return NotFound(new { result = false, code = 404, message = "Không tìm thấy điểm cho MSSV này" });
            }

            var allDiem = diem.SelectMany(d => d.Diems).ToList();
            var (tongDiemHe4TichLuy, tongTinChiTichLuy, tongTinChiDat) = TinhTongHop(allDiem);
            var tichLuyList = TinhTichLuyTheoHocKy(diem);

            var response = new DiemSinhVienResponseViewModel
            {
                Result = true,
                Code = 200,
                Message = "Lấy điểm thành công",
                SoLuong = diem.Count,
                Data = diem,
                TongTinChiTichLuy = tongTinChiTichLuy,
                TongTinChiDat = tongTinChiDat,
                DiemTBTichLuy = tongTinChiTichLuy > 0
                    ? (tongDiemHe4TichLuy / tongTinChiTichLuy).ToString("0.00")
                    : "0.00",
                TichLuyList = tichLuyList
            };

            return Ok(response);
        }


        // Lấy danh sách lớp học phần theo MSSV
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("lophocphansinhvien/{mssv}")]
        public async Task<IActionResult> GetLopHocPhanByMSSV(string mssv)
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
                    message = "Bạn không có quyền truy cập lớp học phần của sinh viên khác"
                });
            }

            //  Truy vấn dữ liệu tương tự SQL bạn viết
            var query = from ct in _context.ChiTietLopHocPhans
                        join sv in _context.SinhViens on ct.SinhVienId equals sv.Id
                        join lhp in _context.LopHocPhans on ct.LopHocPhanId equals lhp.Id
                        join mh in _context.MonHocs on lhp.MonHocId equals mh.Id
                        join hk in _context.HocKys on lhp.HocKyId equals hk.Id
                        where sv.MSSV == mssv
                        group new { mh, lhp } by new { hk.Id, hk.TenHocKy, hk.NgayBatDau } into g
                        orderby g.Key.Id
                        select new
                        {
                            HocKyId = g.Key.Id,
                            TenHocKy = g.Key.TenHocKy,
                            NgayBatDau = g.Key.NgayBatDau,
                            DanhSachMon = g.Select(x => new
                            {
                                x.mh.MaMonHoc,
                                x.mh.TenMonHoc,
                                x.mh.SoTinChi,
                                LopHocPhanId = x.lhp.Id,
                                x.lhp.MaLopHocPhan
                            }).ToList()
                        };

            var data = await query.ToListAsync();

            if (!data.Any())
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy lớp học phần cho MSSV này"
                });
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách lớp học phần thành công",
                soluongHocKy = data.Count,
                data
            });
        }

        //  Lấy các buổi điểm danh của sinh viên theo MSSV và ID lớp học phần
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student + "," + SD.Role_Admin)]
        [HttpGet("lophocphansinhvien/{mssv}/lop/{lopHocPhanId}/diemdanh")]
        public async Task<IActionResult> GetDiemDanhByLop(string mssv, int lopHocPhanId)
        {
            // 1️⃣ Xác thực MSSV từ token
            var mssvFromToken = User.FindFirst("username")?.Value;
            if (mssvFromToken == null)
                return Unauthorized(new { result = false, code = 401, message = "Không lấy được MSSV từ token" });

            if (mssvFromToken != mssv)
                return StatusCode(StatusCodes.Status403Forbidden, new { result = false, code = 403, message = "Không có quyền truy cập dữ liệu của sinh viên khác" });

            // 2️⃣ Kiểm tra sinh viên có thuộc lớp học phần này không
            var isExist = await _context.ChiTietLopHocPhans
                .Include(ct => ct.SinhVien)
                .AnyAsync(ct => ct.LopHocPhanId == lopHocPhanId && ct.SinhVien.MSSV == mssv);

            if (!isExist)
                return NotFound(new { result = false, code = 404, message = "Sinh viên không thuộc lớp học phần này" });

            // 3️⃣ Lấy danh sách buổi điểm danh
            var data = await (from dd in _context.DiemDanhs
                              join ctd in _context.ChiTietDiemDanhs on dd.Id equals ctd.DiemDanhId
                              join sv in _context.SinhViens on ctd.SinhVienId equals sv.Id
                              join tt in _context.TrangThais on ctd.TrangThaiId equals tt.Id into tts
                              from tt in tts.DefaultIfEmpty()
                              where sv.MSSV == mssv && dd.LopHocPhanId == lopHocPhanId
                              orderby dd.Ngay
                              select new
                              {
                                  dd.Ngay,
                                  dd.Code,
                                  dd.GhiChu,
                                  TrangThai = tt != null ? tt.TenTrangThai : "Chưa xác định",
                                  ThoiGian = ctd.ThoiGian,
                                  ctd.Latitude,
                                  ctd.Longitude,
                                  GhiChuChiTiet = ctd.GhiChu
                              }).ToListAsync();

            if (!data.Any())
                return NotFound(new { result = false, code = 404, message = "Không có dữ liệu điểm danh" });

            // 4️⃣ Trả kết quả
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách điểm danh thành công",
                tongSoBuoi = data.Count,
                data
            });
        }



        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student)]
        [HttpPost("diemdanh/checkin")]
        public async Task<IActionResult> CheckinDiemDanh([FromBody] CheckinDiemDanhRequest model)
        {
            var userId = User.FindFirst("userId")?.Value;
            var sinhVien = await _context.SinhViens.FirstOrDefaultAsync(x => x.UserId == userId);
            if (sinhVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy thông tin sinh viên" });

            var trangThaiCoMatId = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "DiemDanh" && t.TenTrangThai == "Có mặt")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var trangThaiBuoiDiemDanhId = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "DiemDanh#" && t.TenTrangThai == "Đang diễn ra")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Buổi hợp lệ + đúng code + còn hạn + đang mở
            var buoi = await _context.DiemDanhs.FirstOrDefaultAsync(x =>
                x.Id == model.DiemDanhId &&
                x.Code == model.Code &&
                x.ExpireAt > DateTime.Now &&
                x.TrangThaiId == trangThaiBuoiDiemDanhId
            );
            if (buoi == null)
                return BadRequest(new { result = false, message = "Mã điểm danh không hợp lệ hoặc đã hết hạn" });

            // Tìm dòng đã seed cho SV trong buổi này
            var ct = await _context.ChiTietDiemDanhs
                .FirstOrDefaultAsync(x => x.DiemDanhId == buoi.Id && x.SinhVienId == sinhVien.Id);

            // Nếu chưa seed (fallback), tạo mới; nếu đã seed thì update
            if (ct == null)
            {
                ct = new ChiTietDiemDanh
                {
                    DiemDanhId = buoi.Id,
                    SinhVienId = sinhVien.Id,
                };
                _context.ChiTietDiemDanhs.Add(ct);
            }
            else
            {
                // Idempotent: nếu đã có mặt rồi thì không cho điểm danh lại
                var coMatId = trangThaiCoMatId;
                if (ct.TrangThaiId == coMatId)
                    return BadRequest(new { result = false, message = "Bạn đã điểm danh buổi này rồi" });
            }

            ct.TrangThaiId = trangThaiCoMatId;
            ct.ThoiGian = DateTime.Now;
            ct.Latitude = model.Latitude;
            ct.Longitude = model.Longitude;
            ct.DeviceId = model.DeviceId;

            await _context.SaveChangesAsync();
            return Ok(new { result = true, message = "Điểm danh thành công!" });
        }


        // Điểm danh qua mã code
        [HttpPost("diemdanh/thuchien")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = SD.Role_Student)]
        public async Task<IActionResult> DiemDanh([FromBody] DiemDanhRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { result = false, message = "Thiếu mã điểm danh" });

            var mssv = User.FindFirst("username")?.Value;
            if (string.IsNullOrEmpty(mssv))
                return Unauthorized(new { result = false, message = "Không tìm thấy MSSV trong token" });

            var now = DateTime.Now;

            // Buổi còn hạn (nên check thêm 'Đang diễn ra' cho đồng nhất)
            var trangThaiBuoiDiemDanhId = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "DiemDanh#" && t.TenTrangThai == "Đang diễn ra")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            var buoi = await _context.DiemDanhs
                .FirstOrDefaultAsync(x => x.Code == request.Code && x.ExpireAt >= now && x.TrangThaiId == trangThaiBuoiDiemDanhId);

            if (buoi == null)
                return BadRequest(new { result = false, message = "Mã điểm danh không hợp lệ hoặc đã hết hạn" });

            var sinhVien = await _context.SinhViens.FirstOrDefaultAsync(x => x.MSSV == mssv);
            if (sinhVien == null)
                return NotFound(new { result = false, message = "Không tìm thấy sinh viên" });

            var inClass = await _context.ChiTietLopHocPhans
                .AnyAsync(x => x.LopHocPhanId == buoi.LopHocPhanId && x.SinhVienId == sinhVien.Id);
            if (!inClass)
                return BadRequest(new { result = false, message = "Sinh viên không thuộc lớp này" });

            var trangThaiCoMatId = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "DiemDanh" && t.TenTrangThai == "Có mặt")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            // Lấy dòng đã seed và update
            var ct = await _context.ChiTietDiemDanhs
                .FirstOrDefaultAsync(x => x.DiemDanhId == buoi.Id && x.SinhVienId == sinhVien.Id);

            if (ct == null)
            {
                ct = new ChiTietDiemDanh
                {
                    DiemDanhId = buoi.Id,
                    SinhVienId = sinhVien.Id,
                };
                _context.ChiTietDiemDanhs.Add(ct);
            }
            else
            {
                if (ct.TrangThaiId == trangThaiCoMatId)
                    return BadRequest(new { result = false, message = "Bạn đã điểm danh buổi này rồi!" });
            }

            ct.TrangThaiId = trangThaiCoMatId;
            ct.Latitude = request.Latitude;
            ct.Longitude = request.Longitude;
            ct.DeviceId = request.DeviceId;
            ct.ThoiGian = now;
            ct.GhiChu = "Điểm danh QR";

            await _context.SaveChangesAsync();

            // Nếu có push Firebase thì giữ nguyên:
            await PushAttendanceToFirebase(buoi.Id, sinhVien, now, trangThaiCoMatId, request);

            return Ok(new { result = true, message = "Điểm danh thành công!" });
        }



        private async Task PushAttendanceToFirebase(
        int diemDanhId,
        SinhVien sv,
        DateTime thoiGian,
        int trangThaiId,
        DiemDanhRequest request)
        {
            var statusText = await _context.TrangThais
                .Where(t => t.Id == trangThaiId)
                .Select(t => t.TenTrangThai)
                .FirstOrDefaultAsync() ?? "Có mặt";

            var fb = new Firebase.Database.FirebaseClient("https://bluenet-e6525-default-rtdb.firebaseio.com");

            var payload = new
            {
                studentId = sv.MSSV,
                studentName = $"{(sv.HoVaTenDem ?? "").Trim()} {(sv.Ten ?? "").Trim()}".Trim(),
                status = statusText,
                statusId = trangThaiId,
                timecheckedin = new[] { "Có mặt", "Đi trễ" }.Contains(statusText) ? thoiGian.ToString("HH:mm") : null,
                bluetoothID = request.DeviceId,
                latitude = request.Latitude,
                longitude = request.Longitude
            };

            await fb.Child("attendancesessions")
                    .Child(diemDanhId.ToString())
                    .Child("students")
                    .Child(sv.MSSV)
                    .PatchAsync(payload);
        }


    }
}