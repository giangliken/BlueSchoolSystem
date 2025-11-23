using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APIAdminController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public APIAdminController(IActivityLogService activityLogService, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _activityLogService = activityLogService;
            _context = context;
            _userManager = userManager;
        }

        //Ghi log hệ thống
        [HttpPost("ghilog")]
        public async Task<IActionResult> Create([FromBody] ActivityLog act)
        {
            await _activityLogService.LogAsync(
                userId: act.UserId,
                userName: act.UserName,
                device: act.Device,
                ipAddress: act.IpAddress,
                actionType: act.ActionType,
                tableName: act.TableName,
                objectId: act.ObjectId,
                description: act.Description
            );
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Ghi log thành công"
            });
        }

        //Xem nhật ký hệ thống
        [Authorize(Roles = "Admin", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("xemnhatky")]
        public IActionResult GetActivityLogs()
        {
            var logs = _activityLogService.GetAllLogs();
            if (logs == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy nhật ký hoạt động"
                });
            }
            return Ok(new
            {
                result = true,
                code = 200,
                data = logs
            });

        }

        //Lấy trạng thái theo loại
        [HttpGet("trangthai/loai/{loai}")]
        public async Task<IActionResult> GetTrangThaiByLoai(string loai)
        {
            if (string.IsNullOrWhiteSpace(loai))
                return BadRequest(new { result = false, message = "Thiếu loại trạng thái" });

            var list = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == loai)
                .OrderBy(t => t.Id)
                .Select(t => new
                {
                    t.Id,
                    t.TenTrangThai,
                    t.MoTa,
                    t.LoaiTrangThai
                })
                .ToListAsync();

            return Ok(new
            {
                result = true,
                message = "Lấy trạng thái thành công",
                soluong = list.Count,
                data = list
            });
        }

        // --- CHỨC NĂNG 1: QUẢN LÝ HỌC KỲ ---

        // Lấy danh sách Học kỳ
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("hocky")]
        public async Task<IActionResult> GetHocKys()
        {
            var hockys = await _context.HocKys
                .Include(h => h.TrangThai)
                .AsNoTracking()
                .ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách học kỳ thành công",
                data = hockys
            });
        }

        // Tạo Học kỳ mới
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("hocky")]
        public async Task<IActionResult> CreateHocKy([FromBody] HocKyDTO dto)
        {
            var trangThaiMoiTao = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "HocKy" && t.TenTrangThai == "Mới tạo");

            if (trangThaiMoiTao == null)
            {
                return BadRequest(new { result = false, message = "Lỗi cấu hình: Không tìm thấy trạng thái 'Mới tạo' cho Học kỳ." });
            }

            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                return BadRequest(new { result = false, message = "Ngày bắt đầu phải trước ngày kết thúc." });
            }

            var newHocKy = new HocKy
            {
                TenHocKy = dto.TenHocKy,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                TrangThaiId = trangThaiMoiTao.Id
            };

            _context.HocKys.Add(newHocKy);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }
            // Ghi log hoạt động
            await _activityLogService.LogAsync(
                userId: userId,
                userName: userName ?? "Unknown",
                device: Request.Headers["User-Agent"].ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                actionType: "CREATE",
                tableName: "HocKys",
                objectId: newHocKy.Id.ToString(),
                description: $"Tạo Học kỳ mới: {newHocKy.TenHocKy}"
            );

            // Tạo thông báo Ngày công bố TKB
            //var thongBaoTKB = new ThongBao
            //{
            //    Title = $"THÔNG BÁO CÔNG BỐ THỜI KHÓA BIỂU {newHocKy.TenHocKy}",
            //    Content = $"Thời khóa biểu chính thức (TKB) cho Học kỳ {newHocKy.TenHocKy} sẽ được công bố vào ngày {dto.NgayCongBoTKB:dd/MM/yyyy}.",
            //    Time = dto.NgayCongBoTKB,
            //    ReceiverUserId = "ALL",
            //    SenderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            //    Type = "TKB_ANNOUNCEMENT"
            //};
            //_context.ThongBaos.Add(thongBaoTKB);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHocKys), new { id = newHocKy.Id }, new
            {
                result = true,
                code = 201,
                message = $"Tạo Học kỳ {newHocKy.TenHocKy} và thông báo TKB thành công."
            });
        }
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("hocky/{id}")]
        public async Task<IActionResult> DeleteHocKy(int id)
        {
            var hocky = await _context.HocKys.FindAsync(id);
            if (hocky == null)
            {
                return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });
            }

            bool isUsedInLopHocPhan = await _context.LopHocPhans.AnyAsync(l => l.HocKyId == id);
            if (isUsedInLopHocPhan)
            {
                return BadRequest(new
                {
                    result = false,
                    message = $"Không thể xóa học kỳ \"{hocky.TenHocKy}\" vì đã được sử dụng trong Lớp học phần."
                });
            }

            bool hasDotDangKy = await _context.DotDangKys.AnyAsync(d => d.HocKyId == id);
            if (hasDotDangKy)
            {
                return BadRequest(new
                {
                    result = false,
                    message = "Không thể xóa học kỳ vì đang có Đợt đăng ký thuộc học kỳ này."
                });
            }
            _context.HocKys.Remove(hocky);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogAsync(
                    userId: userId,
                    userName: userName ?? "Unknown",
                    device: Request.Headers["User-Agent"].ToString(),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                    actionType: "DELETE",
                    tableName: "HocKys",
                    objectId: hocky.Id.ToString(),
                    description: $"Xóa Học kỳ {hocky.TenHocKy}"
                );
            }

            return Ok(new { result = true, code = 200, message = $"Xóa Học kỳ {hocky.TenHocKy} thành công." });
        }
        // Cập nhật thông tin Học kỳ
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("hocky/{id}")]
        public async Task<IActionResult> UpdateHocKy(int id, [FromBody] HocKyDTO dto)
        {
            var hocky = await _context.HocKys.FindAsync(id);
            if (hocky == null)
            {
                return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });
            }

            // Kiểm tra ngày tháng
            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                return BadRequest(new { result = false, message = "Ngày bắt đầu phải trước ngày kết thúc." });
            }

            string oldTenHocKy = hocky.TenHocKy;
            DateTime oldNgayBatDau = hocky.NgayBatDau;
            DateTime oldNgayKetThuc = hocky.NgayKetThuc;

            hocky.TenHocKy = dto.TenHocKy;
            hocky.NgayBatDau = dto.NgayBatDau;
            hocky.NgayKetThuc = dto.NgayKetThuc;

            _context.HocKys.Update(hocky);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogAsync(
                    userId: userId,
                    userName: userName ?? "Unknown",
                    device: Request.Headers["User-Agent"].ToString(),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                    actionType: "UPDATE",
                    tableName: "HocKys",
                    objectId: hocky.Id.ToString(),
                    description: $"Cập nhật Học kỳ {oldTenHocKy} ({oldNgayBatDau:dd/MM/yyyy} - {oldNgayKetThuc:dd/MM/yyyy}) thành {hocky.TenHocKy} ({hocky.NgayBatDau:dd/MM/yyyy} - {hocky.NgayKetThuc:dd/MM/yyyy})"
                );
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = $"Học kỳ {hocky.TenHocKy} đã được cập nhật thành công."
            });
        }
        // Cập nhật Trạng thái Học kỳ
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("hocky/trangthai")]
        public async Task<IActionResult> UpdateHocKyTrangThai([FromBody] UpdateTrangThaiDTO dto)
        {
            var hocky = await _context.HocKys.Include(h => h.TrangThai).FirstOrDefaultAsync(h => h.Id == dto.HocKyId);
            if (hocky == null)
            {
                return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });
            }

            var trangThaiMoi = await _context.TrangThais.FirstOrDefaultAsync(t => t.Id == dto.TrangThaiId && t.LoaiTrangThai == "HocKy");
            if (trangThaiMoi == null)
            {
                return BadRequest(new { result = false, message = "Trạng thái mới không hợp lệ hoặc không thuộc loại 'HocKy'." });
            }

            string oldTrangThai = hocky.TrangThai?.TenTrangThai ?? "N/A";

            hocky.TrangThaiId = trangThaiMoi.Id;
            _context.HocKys.Update(hocky);
            await _context.SaveChangesAsync();

            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }
            // Ghi log hoạt động
            await _activityLogService.LogAsync(
                userId: userId,
                userName: userName ?? "Unknown",
                device: Request.Headers["User-Agent"].ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                actionType: "UPDATE_STATUS",
                tableName: "HocKys",
                objectId: hocky.Id.ToString(),
                description: $"Cập nhật trạng thái Học kỳ {hocky.TenHocKy} từ '{oldTrangThai}' sang '{trangThaiMoi.TenTrangThai}'"
            );

            return Ok(new
            {
                result = true,
                code = 200,
                message = $"Học kỳ {hocky.TenHocKy} đã được chuyển sang trạng thái '{trangThaiMoi.TenTrangThai}' thành công."
            });
        }

        // --- CHỨC NĂNG 2: QUẢN LÝ ĐỢT ĐĂNG KÝ HỌC PHẦN ---

        // Lấy danh sách Đợt Đăng ký theo Học kỳ
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("hocky/{hocKyId}/dotdangky")]
        public async Task<IActionResult> GetDotDangKysByHocKy(int hocKyId)
        {
            var dots = await _context.DotDangKys
                .Where(d => d.HocKyId == hocKyId)
                .OrderBy(d => d.NgayBatDau)
                .AsNoTracking()
                .ToListAsync();

            var result = dots.GroupBy(d => new { d.TenDot, d.NgayBatDau, d.NgayKetThuc, d.LoaiThaoTac })
                .Select(g => new
                {
                    TenDot = g.Key.TenDot,
                    NgayBatDau = g.Key.NgayBatDau,
                    NgayKetThuc = g.Key.NgayKetThuc,
                    LoaiThaoTac = g.Key.LoaiThaoTac,
                    DoiTuongApDungs = g.Select(d => new { d.LoaiDoiTuong, d.GiaTriDoiTuong }).ToList()
                })
                .ToList();

            return Ok(new
            {
                result = true,
                code = 200,
                message = $"Lấy danh sách đợt đăng ký cho HK {hocKyId} thành công",
                data = result
            });
        }

        // Tạo các Đợt Đăng ký Học phần
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("dotdangky")]
        public async Task<IActionResult> CreateDotDangKy([FromBody] DotDangKyDTO dto)
        {
            var hocky = await _context.HocKys.FindAsync(dto.HocKyId);
            if (hocky == null)
            {
                return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });
            }

            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                return BadRequest(new { result = false, message = "Ngày bắt đầu phải trước ngày kết thúc." });
            }

            if (dto.DoiTuongApDungs == null || !dto.DoiTuongApDungs.Any())
            {
                return BadRequest(new { result = false, message = "Phải chọn ít nhất một đối tượng áp dụng." });
            }

            var newDots = new List<DotDangKy>();
            foreach (var doiTuong in dto.DoiTuongApDungs)
            {
                var dot = new DotDangKy
                {
                    HocKyId = dto.HocKyId,
                    TenDot = dto.TenDot,
                    NgayBatDau = dto.NgayBatDau,
                    NgayKetThuc = dto.NgayKetThuc,
                    LoaiDoiTuong = doiTuong.Loai,
                    GiaTriDoiTuong = doiTuong.GiaTri,
                    LoaiThaoTac = dto.LoaiThaoTac
                };
                newDots.Add(dot);
            }

            _context.DotDangKys.AddRange(newDots);
            await _context.SaveChangesAsync();

            // Ghi log
            var logMessage = $"Tạo đợt đăng ký '{dto.TenDot}' ({dto.LoaiThaoTac}) cho HK {hocky.TenHocKy}. Áp dụng cho {dto.DoiTuongApDungs.Count} đối tượng.";
            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }
            await _activityLogService.LogAsync(
                userId: userId,
                userName: userName ?? "Unknown",
                device: Request.Headers["User-Agent"].ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                actionType: "CREATE",
                tableName: "DotDangKys",
                objectId: newDots.First().Id.ToString(),
                description: logMessage
            );

            //// Công bố thông báo
            //var thongBaoDotDK = new ThongBao
            //{
            //    Title = $"THÔNG BÁO ĐỢT ĐĂNG KÝ HỌC PHẦN: {dto.TenDot}",
            //    Content = $"Đợt đăng ký {dto.TenDot} ({dto.LoaiThaoTac}) bắt đầu từ {dto.NgayBatDau:dd/MM/yyyy} đến {dto.NgayKetThuc:dd/MM/yyyy} cho các đối tượng: {string.Join(", ", dto.DoiTuongApDungs.Select(d => d.GiaTri))}.",
            //    Time = DateTime.Now,
            //    ReceiverUserId = "ALL",
            //    SenderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            //    Type = "REG_ANNOUNCEMENT"
            //};
            //_context.ThongBaos.Add(thongBaoDotDK);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDotDangKysByHocKy), new { hocKyId = dto.HocKyId }, new
            {
                result = true,
                code = 201,
                message = $"Tạo đợt đăng ký '{dto.TenDot}' thành công."
            });
        }

        // --- CHỨC NĂNG 3: TẠO & QUẢN LÝ LỚP HỌC PHẦN ---

        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("lophocphan")]
        public async Task<IActionResult> CreateLopHocPhan([FromBody] LopHocPhanCreateDTO dto)
        {
            // 1. Kiểm tra tồn tại các FK
            var hocky = await _context.HocKys.FindAsync(dto.HocKyId);
            var monHoc = await _context.MonHocs.FindAsync(dto.MonHocId);
            var giangVien = await _context.GiangViens.FindAsync(dto.GiangVienId);
            var trangThaiMoiTao = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Đang mở");

            if (hocky == null || monHoc == null || giangVien == null || trangThaiMoiTao == null)
            {
                return NotFound(new { result = false, message = "Thiếu thông tin Học kỳ, Môn học, Giảng viên hoặc Trạng thái cấu hình." });
            }

            // 2. Lấy thông tin người dùng từ Bearer token
            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }

            // 3. Tạo Lớp Học Phần (KHÔNG có lịch học)
            var newLopHP = new LopHocPhan
            {
                HocKyId = dto.HocKyId,
                MonHocId = dto.MonHocId,
                GiangVienId = dto.GiangVienId,
                MaLopHocPhan = dto.MaLopHocPhan,
                TenLopHocPhan = dto.TenLopHocPhan,
                SiSo = dto.SiSoToiDa,
                NgayBatDau = dto.NgayBatDauLHP,
                NgayKetThuc = dto.NgayKetThucLHP,
                TrangThaiId = trangThaiMoiTao.Id
            };

            _context.LopHocPhans.Add(newLopHP);
            await _context.SaveChangesAsync();

            // 4. Ghi log hoạt động
            await _activityLogService.LogAsync(
                userId: userId,
                userName: userName ?? "Unknown",
                device: Request.Headers["User-Agent"].ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                actionType: "CREATE",
                tableName: "LopHocPhans",
                objectId: newLopHP.Id.ToString(),
                description: $"Tạo Lớp Học Phần: {newLopHP.MaLopHocPhan} ({newLopHP.TenLopHocPhan})"
            );

            // 5. Trả kết quả
            return CreatedAtAction(nameof(GetLopHocPhans), new { id = newLopHP.Id }, new
            {
                result = true,
                code = 201,
                message = $"Tạo Lớp Học Phần {newLopHP.MaLopHocPhan} thành công.",
                lopHocPhanId = newLopHP.Id
            });
        }



        [HttpPost("lophocphan/lichhoc")]
        public async Task<IActionResult> CreateLichHoc([FromBody] LichHocDTO dto)
        {
            var lhp = await _context.LopHocPhans
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(l => l.Id == dto.LopHocPhanId);

            if (lhp == null)
                return NotFound(new { result = false, message = $"Không tìm thấy Lớp Học Phần với Id = {dto.LopHocPhanId}" });

            int giangVienId = (int)lhp.GiangVienId;

            // 1. Kiểm tra trùng lịch Giảng viên
            var isGVConflict = await IsGiangVienConflict(giangVienId, dto.Ngay, dto.GioBatDau, dto.GioKetThuc);
            if (isGVConflict)
                return BadRequest(new { result = false, message = "Giảng viên bị trùng lịch." });

            // 2. Kiểm tra trùng lịch Phòng
            var isRoomConflict = await IsPhongHocConflict(dto.PhongHocId, dto.Ngay, dto.GioBatDau, dto.GioKetThuc);
            if (isRoomConflict)
                return BadRequest(new { result = false, message = "Phòng học bị trùng lịch." });

            // 3. Tạo lịch học
            var lichHoc = new LichHoc
            {
                LopHocPhanId = dto.LopHocPhanId,
                Ngay = dto.Ngay.Date,  // lấy ngày từ DTO
                GioBatDau = dto.GioBatDau,
                GioKetThuc = dto.GioKetThuc,
                PhongHocId = dto.PhongHocId
            };

            _context.LichHocs.Add(lichHoc);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Tạo lịch học thành công."
            });
        }

        // API 7: Hủy / Mở lại lớp học phần
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("lophocphan/trangthai")]
        public async Task<IActionResult> UpdateLopHocPhanStatus([FromBody] UpdateLopHocPhanStatusDTO dto)
        {
            var lhp = await _context.LopHocPhans.Include(l => l.TrangThai).FirstOrDefaultAsync(l => l.Id == dto.LopHocPhanId);
            if (lhp == null)
            {
                return NotFound(new { result = false, message = "Không tìm thấy Lớp Học Phần." });
            }

            var trangThaiMoi = await _context.TrangThais.FirstOrDefaultAsync(t => t.Id == dto.TrangThaiId && t.LoaiTrangThai == "LopHocPhan");
            if (trangThaiMoi == null)
            {
                return BadRequest(new { result = false, message = "Trạng thái mới không hợp lệ." });
            }

            string oldTrangThai = lhp.TrangThai?.TenTrangThai ?? "N/A";
            lhp.TrangThaiId = trangThaiMoi.Id;
            _context.LopHocPhans.Update(lhp);
            await _context.SaveChangesAsync();

            // Nếu lớp bị Hủy, cần tạo thông báo (Phải công bố ngày 09/12)
            //if (trangThaiMoi.TenTrangThai.Equals("BiHuy", StringComparison.OrdinalIgnoreCase))
            //{
            //    // Logic thông báo lớp hủy
            //    var thongBaoHuyLop = new ThongBao
            //    {
            //        Title = $"THÔNG BÁO HỦY LỚP HỌC PHẦN: {lhp.MaLopHocPhan}",
            //        Content = $"Lớp học phần {lhp.MaLopHocPhan} ({lhp.TenLopHocPhan}) đã bị hủy vì lý do: {dto.LyDo}. Ngày công bố hủy lớp là 09/12. Sinh viên vui lòng đăng ký lại.",
            //        Time = DateTime.Now,
            //        ReceiverUserId = "STUDENTS_IN_LHP",
            //        SenderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            //        Type = "LHP_CANCEL"
            //    };
            //    _context.ThongBaos.Add(thongBaoHuyLop);
            //    await _context.SaveChangesAsync();
            //}

            // Ghi log hoạt động
            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }
            await _activityLogService.LogAsync(
                userId: userId,
                userName: userName ?? "Unknown",
                device: Request.Headers["User-Agent"].ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                actionType: "UPDATE_STATUS",
                tableName: "LopHocPhans",
                objectId: lhp.Id.ToString(),
                description: $"Cập nhật trạng thái LHP {lhp.MaLopHocPhan} từ '{oldTrangThai}' sang '{trangThaiMoi.TenTrangThai}'."
            );

            return Ok(new
            {
                result = true,
                code = 200,
                message = $"Lớp Học Phần {lhp.MaLopHocPhan} đã được cập nhật trạng thái thành công."
            });
        }

        // API 8: Lấy danh sách LHP (để Admin quản lý/xem sĩ số)
        [HttpGet("lophocphans")]
        public async Task<IActionResult> GetLopHocPhans()
        {
            // Cần tính sĩ số thực tế (tổng số sinh viên đăng ký)
            var lhpList = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .Include(l => l.TrangThai)
                .Select(l => new
                {
                    l.Id,
                    l.MaLopHocPhan,
                    TenLop = l.TenLopHocPhan,
                    MonHoc = l.MonHoc.TenMonHoc,
                    GiangVien = l.GiangVien.HoVaTenDem + " " + l.GiangVien.Ten,
                    SiSoToiDa = l.SiSo,
                    SiSoThucTe = _context.DangKyHocPhans.Count(dk => dk.LopHocPhanId == l.Id), // Tính Sĩ số
                    TrangThai = l.TrangThai.TenTrangThai, // Tình trạng lớp: đủ mở / không đủ
                    LichHoc = _context.LichHocs.Where(lh => lh.LopHocPhanId == l.Id).Select(lh => new
                    {
                        lh.Ngay,
                        lh.GioBatDau,
                        lh.GioKetThuc,
                        Phong = lh.PhongHocId.ToString() // Có thể join thêm tên phòng
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = "Lấy danh sách Lớp Học Phần thành công",
                data = lhpList
            });
        }

        // --- HÀM KIỂM TRA TRÙNG LỊCH NỘI BỘ ---

        // Kiểm tra Giảng viên có trùng lịch với LHP khác không
        private async Task<bool> IsGiangVienConflict(int giangVienId, DateTime ngay, TimeSpan gioBatDau, TimeSpan gioKetThuc)
        {
            var lichGV = await _context.LichHocs
                .Join(_context.LopHocPhans,
                      l => l.LopHocPhanId,
                      lp => lp.Id,
                      (l, lp) => new { LichHoc = l, LopHocPhan = lp })
                .Where(x => x.LopHocPhan.GiangVienId == giangVienId)
                .AsNoTracking()
                .ToListAsync();

            return lichGV.Any(x =>
                x.LichHoc.Ngay.Date == ngay.Date &&
                x.LichHoc.GioBatDau < gioKetThuc &&
                x.LichHoc.GioKetThuc > gioBatDau
            );
        }
        // Kiểm tra Phòng học có trùng lịch không
        private async Task<bool> IsPhongHocConflict(int phongHocId, DateTime ngay, TimeSpan gioBatDau, TimeSpan gioKetThuc)
        {
            var lichPhong = await _context.LichHocs
                .Where(l => l.PhongHocId == phongHocId)
                .AsNoTracking()
                .ToListAsync();

            return lichPhong.Any(l =>
                l.Ngay.Date == ngay.Date &&
                l.GioBatDau < gioKetThuc &&
                l.GioKetThuc > gioBatDau
            );
        }

        // --- CHỨC NĂNG 4: AUTO ĐĂNG KÝ HỌC PHẦN BẮT BUỘC ---

        // API: Kích hoạt Job tự động đăng ký HP bắt buộc
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("auto-dangky-batbuoc/{hocKyId}")]
        public async Task<IActionResult> RunAutoDangKyBatBuoc(int hocKyId, [FromQuery] int thuTuHocKy)
        {
            // 1. Lấy thông tin học kỳ thực tế
            var hocKy = await _context.HocKys.FindAsync(hocKyId);
            if (hocKy == null)
                return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });

            int successCount = 0;
            int errorCount = 0;

            // 2. Lấy danh sách sinh viên đang học
            var sinhViens = await _context.SinhViens
                .Include(sv => sv.Lop)
                .Where(sv => sv.TrangThai.TenTrangThai == "Đang học")
                .ToListAsync();

            foreach (var sv in sinhViens)
            {
                if (sv.Lop?.NganhId == null)
                    continue; // bỏ qua sinh viên chưa có ngành

                // 3. Lấy danh sách môn bắt buộc theo thứ tự học kỳ và ngành
                var ctBatBuoc = await _context.ChiTietChuongTrinhDaoTaos
                    .Where(ct => ct.BatBuoc &&
                                 ct.HocKy == thuTuHocKy &&
                                 ct.ChuongTrinhDaoTao.NganhHocId == sv.Lop.NganhId)
                    .ToListAsync();

                foreach (var monCt in ctBatBuoc)
                {
                    // 4. Tìm lớp học phần còn chỗ
                    var lhpTuongUng = await _context.LopHocPhans
                        .Where(l => l.MonHoc.MaMonHoc == monCt.MaMonHoc &&
                                    l.HocKyId == hocKyId &&
                                    l.TrangThai.TenTrangThai == "Đang mở" &&
                                    l.SiSo > _context.DangKyHocPhans.Count(dk => dk.LopHocPhanId == l.Id))
                        .FirstOrDefaultAsync();

                    if (lhpTuongUng != null)
                    {
                        // 5. Kiểm tra sinh viên đã đăng ký chưa
                        var isRegistered = await _context.DangKyHocPhans
                            .AnyAsync(dk => dk.SinhVienId == sv.Id && dk.LopHocPhanId == lhpTuongUng.Id);

                        if (!isRegistered)
                        {
                            _context.DangKyHocPhans.Add(new DangKyHocPhan
                            {
                                SinhVienId = sv.Id,
                                LopHocPhanId = lhpTuongUng.Id,
                                NgayDangKy = DateTime.Now,
                                LoaiDangKy = "BatBuocAuto"
                            });
                            successCount++;
                        }
                    }
                    else
                    {
                        errorCount++;
                    }
                }
            }

            // 6. Lưu thay đổi
            await _context.SaveChangesAsync();

            // 7. Ghi log hoạt động
            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value ?? "Unknown";

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });

            await _activityLogService.LogAsync(
                userId: userId,
                userName: userName,
                device: Request.Headers["User-Agent"].ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                actionType: "JOB_RUN",
                tableName: "DangKyHocPhans",
                objectId: hocKyId.ToString(),
                description: $"Chạy Job Auto Đăng ký HP Bắt buộc cho Học kỳ {hocKy.TenHocKy} " +
                             $"(ThuTuHocKy={thuTuHocKy}). Thành công: {successCount}, Lỗi: {errorCount}"
            );

            // 8. Trả kết quả
            return Ok(new
            {
                result = true,
                code = 200,
                message = $"Job Auto Đăng ký HP Bắt buộc cho Học kỳ {hocKy.TenHocKy} (ThuTuHocKy={thuTuHocKy}) đã hoàn thành. " +
                          $"Thành công: {successCount} đăng ký, Lỗi: {errorCount} môn không tìm thấy lớp."
            });
        }


        //// --- CHỨC NĂNG 5: QUẢN LÝ TÀI KHOẢN SINH VIÊN ---

        //// API 10: Reset mật khẩu cho Sinh viên (khi SV không đăng nhập được)
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpPost("user/reset-password")]
        //public async Task<IActionResult> ResetStudentPassword([FromBody] PasswordResetDTO dto)
        //{
        //    var user = await _userManager.FindByIdAsync(dto.UserId);
        //    if (user == null)
        //    {
        //        return NotFound(new { result = false, message = "Không tìm thấy người dùng." });
        //    }

        //    // Xóa Token cũ và tạo Token mới để đặt lại mật khẩu
        //    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        //    // Thực hiện Reset mật khẩu
        //    var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        //    if (!result.Succeeded)
        //    {
        //        return BadRequest(new { result = false, message = "Không thể reset mật khẩu.", errors = result.Errors });
        //    }

        //    // Ghi log hoạt động
        //    await _activityLogService.LogAsync(
        //        userId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        //        userName: User.FindFirstValue(ClaimTypes.Name)!,
        //        device: Request.Headers["User-Agent"].ToString(),
        //        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //        actionType: "PASSWORD_RESET",
        //        tableName: "ApplicationUsers",
        //        objectId: user.Id,
        //        description: $"Reset mật khẩu thành công cho người dùng: {user.UserName}"
        //    );

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = $"Reset mật khẩu thành công cho sinh viên {user.UserName}."
        //    });
        //}

        //// API 11: Khóa/Mở tài khoản SV
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpPost("user/lockout")]
        //public async Task<IActionResult> LockUnlockAccount([FromBody] AccountLockoutDTO dto)
        //{
        //    var user = await _userManager.FindByIdAsync(dto.UserId);
        //    if (user == null)
        //    {
        //        return NotFound(new { result = false, message = "Không tìm thấy người dùng." });
        //    }

        //    if (dto.IsLock)
        //    {
        //        // Khóa tài khoản
        //        var lockoutEnd = DateTimeOffset.UtcNow.AddDays(dto.LockoutDays);
        //        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        //        if (!result.Succeeded)
        //        {
        //            return BadRequest(new { result = false, message = "Không thể khóa tài khoản.", errors = result.Errors });
        //        }

        //        await _activityLogService.LogAsync(
        //            userId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        //            userName: User.FindFirstValue(ClaimTypes.Name)!,
        //            device: Request.Headers["User-Agent"].ToString(),
        //            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //            actionType: "LOCK_ACCOUNT",
        //            tableName: "ApplicationUsers",
        //            objectId: user.Id,
        //            description: $"Khóa tài khoản {user.UserName}. Lý do: {dto.Reason}"
        //        );

        //        return Ok(new { result = true, code = 200, message = $"Khóa tài khoản {user.UserName} đến ngày {lockoutEnd:dd/MM/yyyy HH:mm:ss}." });
        //    }
        //    else
        //    {
        //        // Mở tài khoản
        //        var result = await _userManager.SetLockoutEndDateAsync(user, null);

        //        if (!result.Succeeded)
        //        {
        //            return BadRequest(new { result = false, message = "Không thể mở tài khoản.", errors = result.Errors });
        //        }

        //        await _activityLogService.LogAsync(
        //            userId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        //            userName: User.FindFirstValue(ClaimTypes.Name)!,
        //            device: Request.Headers["User-Agent"].ToString(),
        //            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //            actionType: "UNLOCK_ACCOUNT",
        //            tableName: "ApplicationUsers",
        //            objectId: user.Id,
        //            description: $"Mở tài khoản {user.UserName}."
        //        );

        //        return Ok(new { result = true, code = 200, message = $"Mở tài khoản {user.UserName} thành công." });
        //    }
        //}

        //// API 12: Kiểm soát trạng thái đã đổi mật khẩu & đã khai báo email
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpGet("sinhvien/pending-onboarding")]
        //public async Task<IActionResult> GetPendingOnboardingStudents()
        //{
        //    var pendingUsers = await _context.Users
        //        .Where(u => u.EmailConfirmed == false || u.FaceRegistered == false)
        //        .Select(u => new
        //        {
        //            u.Id,
        //            u.UserName,
        //            u.Email,
        //            EmailConfirmed = u.EmailConfirmed,
        //            IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
        //            SinhVienId = u.SinhViens != null ? u.SinhViens.MSSV : "N/A"
        //        })
        //        .ToListAsync();

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = "Danh sách sinh viên cần hoàn thành Onboarding",
        //        data = pendingUsers
        //    });
        //}

        //// --- CHỨC NĂNG 6: XÁC NHẬN - PHÊ DUYỆT THAO TÁC CỦA SINH VIÊN ---

        //// API 13: Xử lý Hủy Đăng ký Học phần
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpPost("dangky/huy")]
        //public async Task<IActionResult> ApproveHuyDangKy([FromBody] XacNhanDangKyHocPhanDTO dto)
        //{
        //    // 1. Kiểm tra thời gian hợp lệ: 15/12 -> 26/12 (Giả định đang trong năm hiện tại)
        //    var now = DateTime.Now;
        //    var ngayHuyBatDau = new DateTime(now.Year, 12, 15);
        //    var ngayHuyKetThuc = new DateTime(now.Year, 12, 26);

        //    if (now < ngayHuyBatDau || now > ngayHuyKetThuc)
        //    {
        //        return BadRequest(new { result = false, message = $"Thời gian hủy không hợp lệ. Chỉ cho phép từ {ngayHuyBatDau:dd/MM} đến {ngayHuyKetThuc:dd/MM}." });
        //    }

        //    // 2. Tìm bản ghi đăng ký
        //    var dangKy = await _context.DangKyHocPhans
        //        .FirstOrDefaultAsync(dk => dk.Id == dto.DangKyHocPhanId && dk.SinhVienId == dto.SinhVienId);

        //    if (dangKy == null)
        //    {
        //        return NotFound(new { result = false, message = "Không tìm thấy bản ghi đăng ký học phần." });
        //    }

        //    // 3. Thực hiện Hủy: Xóa
        //    _context.DangKyHocPhans.Remove(dangKy);
        //    await _context.SaveChangesAsync();

        //    // 4. Logic: Khi hủy, lớp này không thu học phí.

        //    await _activityLogService.LogAsync(
        //        userId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        //        userName: User.FindFirstValue(ClaimTypes.Name)!,
        //        device: Request.Headers["User-Agent"].ToString(),
        //        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //        actionType: "HUY_DKHP",
        //        tableName: "DangKyHocPhans",
        //        objectId: dangKy.Id.ToString(),
        //        description: $"Xác nhận Hủy ĐKHP Id: {dangKy.Id} cho SV Id: {dto.SinhVienId}. Lý do: {dto.LyDo}"
        //    );

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = "Hủy đăng ký học phần thành công. **Không thu học phí** cho môn này."
        //    });
        //}

        //// API 14: Xử lý Rút Đăng ký Học phần
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpPost("dangky/rut")]
        //public async Task<IActionResult> ApproveRutDangKy([FromBody] XacNhanDangKyHocPhanDTO dto)
        //{
        //    // 1. Kiểm tra thời gian: 19/01 -> 08/02 (Giả định trong năm tiếp theo)
        //    var nextYear = DateTime.Now.Year + 1;
        //    var now = DateTime.Now;
        //    var ngayRutBatDau = new DateTime(nextYear, 01, 19);
        //    var ngayRutKetThuc = new DateTime(nextYear, 02, 08);

        //    // Trường hợp chạy job vào cuối năm trước
        //    if (now.Month >= 11)
        //    {
        //        ngayRutBatDau = new DateTime(now.Year + 1, 01, 19);
        //        ngayRutKetThuc = new DateTime(now.Year + 1, 02, 08);
        //    }

        //    if (now < ngayRutBatDau || now > ngayRutKetThuc)
        //    {
        //        return BadRequest(new { result = false, message = $"Thời gian rút không hợp lệ. Chỉ cho phép từ {ngayRutBatDau:dd/MM} đến {ngayRutKetThuc:dd/MM}." });
        //    }

        //    // 2. Tìm bản ghi đăng ký
        //    var dangKy = await _context.DangKyHocPhans
        //        .FirstOrDefaultAsync(dk => dk.Id == dto.DangKyHocPhanId && dk.SinhVienId == dto.SinhVienId);

        //    if (dangKy == null)
        //    {
        //        return NotFound(new { result = false, message = "Không tìm thấy bản ghi đăng ký học phần." });
        //    }

        //    // 3. Thực hiện Rút: Đánh dấu là đã rút
        //    // Không hoàn học phí
        //    dangKy.LoaiDangKy = "RUT";

        //    _context.DangKyHocPhans.Update(dangKy);
        //    await _context.SaveChangesAsync();

        //    await _activityLogService.LogAsync(
        //        userId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        //        userName: User.FindFirstValue(ClaimTypes.Name)!,
        //        device: Request.Headers["User-Agent"].ToString(),
        //        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //        actionType: "RUT_DKHP",
        //        tableName: "DangKyHocPhans",
        //        objectId: dangKy.Id.ToString(),
        //        description: $"Xác nhận Rút ĐKHP Id: {dangKy.Id} cho SV Id: {dto.SinhVienId}. Lý do: {dto.LyDo}"
        //    );

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = "Rút đăng ký học phần thành công. **Không hoàn học phí**."
        //    });
        //}

        //// API 15: Phê duyệt Đặc biệt (Bảo lưu, Miễn học, Chuyển điểm)
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpPost("pheduyet-dacbiet")]
        //public async Task<IActionResult> PheDuyetDacBiet([FromBody] PheDuyetDacBietDTO dto)
        //{
        //    var sv = await _context.SinhViens.FindAsync(dto.SinhVienId);
        //    if (sv == null)
        //    {
        //        return NotFound(new { result = false, message = "Không tìm thấy Sinh viên." });
        //    }

        //    string actionType = $"PHEDUYET_{dto.LoaiYeuCau.ToUpper()}";
        //    string logDesc = $"{(dto.IsApproved ? "Phê duyệt" : "Từ chối")} yêu cầu {dto.LoaiYeuCau} cho SV {sv.MSSV}. Nội dung: {dto.NoiDungChiTiet}";

        //    // Logic thực tế (Cần triển khai thêm logic cập nhật trạng thái SV/thêm bản ghi Miễn học/Chuyển điểm)

        //    await _activityLogService.LogAsync(
        //        userId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
        //        userName: User.FindFirstValue(ClaimTypes.Name)!,
        //        device: Request.Headers["User-Agent"].ToString(),
        //        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //        actionType: actionType,
        //        tableName: "SinhViens",
        //        objectId: dto.SinhVienId.ToString(),
        //        description: logDesc
        //    );

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = logDesc.Replace("SV", $"Sinh viên {sv.MSSV}")
        //    });
        //}

        //// --- CHỨC NĂNG 7: CÔNG BỐ THÔNG TIN CHO SINH VIÊN ---

        //// API 16: Lấy tất cả thông tin cần công bố cho sinh viên
        //[AllowAnonymous]
        //[HttpGet("thongtin-congbo/{hocKyId}")]
        //public async Task<IActionResult> GetThongTinCongBo(int hocKyId)
        //{
        //    var hocky = await _context.HocKys.FindAsync(hocKyId);
        //    if (hocky == null)
        //    {
        //        return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });
        //    }

        //    // 1. Lấy các Thông báo chung (Thông báo đợt đăng ký)
        //    var thongBaoChung = await _context.ThongBaos
        //        .Where(t => t.ReceiverUserId == "ALL" || t.Type == "REG_ANNOUNCEMENT" || t.Type == "TKB_ANNOUNCEMENT")
        //        .OrderByDescending(t => t.Time)
        //        .Take(10)
        //        .ToListAsync();

        //    // 2. Lấy Danh sách Lớp mở/hủy (09/12)
        //    var danhSachLHP = await _context.LopHocPhans
        //        .Where(l => l.HocKyId == hocKyId)
        //        .Include(l => l.MonHoc)
        //        .Include(l => l.GiangVien)
        //        .Include(l => l.TrangThai)
        //        .Select(l => new
        //        {
        //            MaLopHocPhan = l.MaLopHocPhan,
        //            TenMonHoc = l.MonHoc!.TenMonHoc,
        //            GiangVien = l.GiangVien!.HoVaTenDem + " " + l.GiangVien.Ten,
        //            TrangThai = l.TrangThai!.TenTrangThai,
        //            GhiChuHuy = l.TrangThai.TenTrangThai.Equals("BiHuy") ? $"Lớp hủy được công bố ngày 09/12" : null
        //        })
        //        .ToListAsync();

        //    // 3. Lấy Thời khóa biểu chính thức
        //    var tkbChinhThuc = await _context.LichHocs
        //        .Where(lh => lh.LopHocPhan.HocKyId == hocKyId)
        //        .Include(lh => lh.LopHocPhan)
        //        .Select(lh => new
        //        {
        //            LopHP = lh.LopHocPhan.MaLopHocPhan,
        //            Ngay = lh.Ngay,
        //            GioBatDau = lh.GioBatDau,
        //            GioKetThuc = lh.GioKetThuc,
        //            Phong = lh.PhongHocId.ToString() // Cần join thêm PhongHoc
        //        })
        //        .ToListAsync();

        //    // 4. Lấy Lịch thi
        //    var lichThi = await _context.LichThis
        //        .Where(lt => lt.LopHocPhan.HocKyId == hocKyId)
        //        .Include(lt => lt.TrangThai)
        //        .Select(lt => new
        //        {
        //            LopHP = lt.LopHocPhan.MaLopHocPhan,
        //            NgayThi = lt.NgayThi,
        //            GioBatDau = lt.GioBatDau,
        //            PhongThi = lt.PhongHoc.MaPhongHoc, // Cần join thêm PhongHoc
        //            TrangThai = lt.TrangThai!.TenTrangThai
        //        })
        //        .ToListAsync();

        //    var result = new
        //    {
        //        ThongBaoChung = thongBaoChung,
        //        LopHocPhanCongBo = danhSachLHP,
        //        ThoiKhoaBieu = tkbChinhThuc,
        //        LichThi = lichThi
        //    };

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = $"Công bố thông tin Học kỳ {hocky.TenHocKy} thành công.",
        //        data = result
        //    });
        //}


        //// --- CHỨC NĂNG 8: HỖ TRỢ SINH VIÊN TRONG THỜI GIAN ĐĂNG KÝ ---

        ////// API 17: Lấy danh sách các yêu cầu hỗ trợ từ sinh viên
        ////[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Staff, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        ////[HttpGet("hotro/yeucau")]
        ////public async Task<IActionResult> GetYeuCauHoTro()
        ////{
        ////    var yeuCauHoTro = await _context.ActivityLogs
        ////        .Where(a => a.ActionType == "ERROR_REGISTER" || a.ActionType == "SUPPORT_REQUEST" || a.ActionType == "PASSWORD_RESET")
        ////        .OrderByDescending(a => a.Timestamp)
        ////        .Take(50)
        ////        .Select(a => new
        ////        {
        ////            a.Id,
        ////            ThoiGian = a.Timestamp,
        ////            NguoiDung = a.UserName,
        ////            HanhDong = a.ActionType,
        ////            MoTa = a.Description,
        ////            CanThiep = a.ActionType switch
        ////            {
        ////                "PASSWORD_RESET" => "Reset tài khoản",
        ////                "ERROR_REGISTER" => "Xử lý lỗi đăng ký/trùng lịch",
        ////                _ => "Can thiệp khi SV báo lỗi hệ thống"
        ////            }
        ////        })
        ////        .ToListAsync();

        ////    return Ok(new
        ////    {
        ////        result = true,
        ////        code = 200,
        ////        message = "Danh sách yêu cầu hỗ trợ cần xử lý (Trực chat / hotline)",
        ////        data = yeuCauHoTro
        ////    });
        ////}


        //// --- CHỨC NĂNG 9: QUẢN LÝ BÁO CÁO & THỐNG KÊ ---

        //// API 18: Thống kê số lượng đăng ký theo Ngành/Khóa
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpGet("baocao/dangky-nganh-khoa")]
        //public async Task<IActionResult> GetReportDangKyByNganhKhoa()
        //{
        //    var report = await _context.DangKyHocPhans
        //        .Include(dk => dk.SinhVien)
        //            .ThenInclude(sv => sv!.Lop)
        //                .ThenInclude(l => l!.Nganh)
        //        .GroupBy(dk => new { dk.SinhVien!.Lop!.Nganh!.TenNganh, KhoaHoc = dk.SinhVien.MSSV.Substring(0, 4) })
        //        .Select(g => new
        //        {
        //            TenNganh = g.Key.TenNganh,
        //            KhoaHoc = g.Key.KhoaHoc,
        //            TongSoSinhVienDangKy = g.Select(dk => dk.SinhVienId).Distinct().Count(),
        //            TongSoHocPhanDangKy = g.Count()
        //        })
        //        .OrderBy(r => r.KhoaHoc)
        //        .ThenBy(r => r.TenNganh)
        //        .ToListAsync();

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = "Báo cáo số sinh viên đăng ký theo ngành/khóa thành công.",
        //        data = report
        //    });
        //}

        //// API 19: Thống kê Hủy / Rút Học phần
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpGet("baocao/huy-rut")]
        //public async Task<IActionResult> GetReportHuyRutHocPhan()
        //{
        //    var huyRutStats = await _context.DangKyHocPhans
        //        .Where(dk => dk.LoaiDangKy == "RUT" || dk.LoaiDangKy == "HUY")
        //        .GroupBy(dk => dk.LoaiDangKy)
        //        .Select(g => new
        //        {
        //            LoaiThaoTac = g.Key,
        //            TongSoLanThucHien = g.Count()
        //        })
        //        .ToListAsync();

        //    var tongHuy = huyRutStats.FirstOrDefault(s => s.LoaiThaoTac == "HUY")?.TongSoLanThucHien ?? 0;
        //    var tongRut = huyRutStats.FirstOrDefault(s => s.LoaiThaoTac == "RUT")?.TongSoLanThucHien ?? 0;

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = "Thống kê Hủy / Rút Học phần thành công.",
        //        data = new
        //        {
        //            TongHuy = tongHuy,
        //            TongRut = tongRut,
        //            ChiTiet = huyRutStats
        //        }
        //    });
        //}

        //// API 20: Báo cáo trùng lịch
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpGet("baocao/trung-lich/{hocKyId}")]
        //public async Task<IActionResult> GetReportTrungLich(int hocKyId)
        //{
        //    var lichHocTrongKy = await _context.LichHocs
        //        .Where(lh => lh.LopHocPhan.HocKyId == hocKyId)
        //        .Include(lh => lh.LopHocPhan)
        //        .ToListAsync();

        //    var conflictReport = new List<object>();

        //    // Nhóm theo Ngày và Thời gian
        //    var conflictCandidates = lichHocTrongKy
        //        .GroupBy(lh => new { DayOfWeek = (int)lh.Ngay.DayOfWeek, lh.GioBatDau, lh.GioKetThuc })
        //        .ToList();

        //    foreach (var group in conflictCandidates)
        //    {
        //        // 1. Kiểm tra trùng Phòng
        //        var roomConflicts = group.GroupBy(lh => lh.PhongHocId)
        //            .Where(g => g.Count() > 1)
        //            .Select(g => new
        //            {
        //                LoaiTrung = "Trùng Phòng",
        //                PhongId = g.Key,
        //                LopHocPhans = g.Select(lh => lh.LopHocPhan.MaLopHocPhan).ToList()
        //            })
        //            .ToList();

        //        // 2. Kiểm tra trùng Giảng viên
        //        var gvConflicts = group.GroupBy(lh => lh.LopHocPhan.GiangVienId)
        //            .Where(g => g.Count() > 1 && g.Key.HasValue)
        //            .Select(g => new
        //            {
        //                LoaiTrung = "Trùng Giảng Viên",
        //                GiangVienId = g.Key,
        //                LopHocPhans = g.Select(lh => lh.LopHocPhan.MaLopHocPhan).ToList()
        //            })
        //            .ToList();

        //        if (roomConflicts.Any() || gvConflicts.Any())
        //        {
        //            conflictReport.Add(new
        //            {
        //                NgayTrongTuan = group.Key.DayOfWeek,
        //                GioBatDau = group.Key.GioBatDau,
        //                GioKetThuc = group.Key.GioKetThuc,
        //                Conflicts = roomConflicts.Cast<object>().Concat(gvConflicts).ToList()
        //            });
        //        }
        //    }

        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = "Báo cáo trùng lịch thành công.",
        //        data = conflictReport
        //    });
        //}
    }
}