using Azure.Core;
using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Repository;
using BlueSchoolSystem.Services;
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
        private readonly LopHocPhanService _lopHocPhanService;
        private readonly IEmailSender _emailSender;

        public APIAdminController(IActivityLogService activityLogService, ApplicationDbContext context, UserManager<ApplicationUser> userManager,LopHocPhanService lopHocPhanService, IEmailSender emailSender)
        {
            _activityLogService = activityLogService;
            _context = context;
            _userManager = userManager;
            _lopHocPhanService = lopHocPhanService;
            _emailSender = emailSender;
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
            // 1. Kiểm tra tồn tại các FK (Giữ nguyên)
            var hocky = await _context.HocKys.FindAsync(dto.HocKyId);
            var monHoc = await _context.MonHocs.FindAsync(dto.MonHocId);
            var giangVien = await _context.GiangViens.FindAsync(dto.GiangVienId);
            var trangThaiMoiTao = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Chờ mở");

            if (hocky == null || monHoc == null || giangVien == null || trangThaiMoiTao == null)
            {
                return NotFound(new { result = false, message = "Thiếu thông tin Học kỳ, Môn học, Giảng viên hoặc Trạng thái cấu hình." });
            }

            // 2. Tạo Lớp Học Phần (Lưu trước để lấy Id)
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

            // --- BỔ SUNG LOGIC TỰ ĐỘNG THÊM LỊCH HỌC ---
            int schedulesCreated = 0;
            int conflictCount = 0;

            if (dto.ShouldAutoCreateSchedule == true &&
                dto.AutoGioBatDau.HasValue && dto.AutoGioKetThuc.HasValue &&
                dto.AutoPhongHocId.HasValue && dto.AutoCacNgayTrongTuan != null &&
                dto.AutoCacNgayTrongTuan.Any())
            {
                var currentDate = newLopHP.NgayBatDau;
                var endDate = newLopHP.NgayKetThuc;

                // Chức năng chuyển đổi DayOfWeek sang định dạng tùy chỉnh của bạn
                // (Bạn cần phải có hàm này trong API Controller, ví dụ: ConvertDayOfWeekToCustomDay)
                int ConvertDayOfWeekToCustomDay(DayOfWeek dayOfWeek)
                {
                    return dayOfWeek == DayOfWeek.Sunday ? 8 : (int)dayOfWeek + 1;
                }

                while (currentDate <= endDate)
                {
                    var customDayOfWeek = ConvertDayOfWeekToCustomDay(currentDate.DayOfWeek);

                    if (dto.AutoCacNgayTrongTuan.Contains(customDayOfWeek))
                    {
                        // Kiểm tra trùng lịch Giảng viên
                        var isGVConflict = await IsGiangVienConflict((int)newLopHP.GiangVienId, currentDate, dto.AutoGioBatDau.Value, dto.AutoGioKetThuc.Value);

                        // Kiểm tra trùng lịch Phòng
                        var isRoomConflict = await IsPhongHocConflict(dto.AutoPhongHocId.Value, currentDate, dto.AutoGioBatDau.Value, dto.AutoGioKetThuc.Value);

                        if (!isGVConflict && !isRoomConflict)
                        {
                            _context.LichHocs.Add(new LichHoc
                            {
                                LopHocPhanId = newLopHP.Id,
                                Ngay = currentDate,
                                GioBatDau = dto.AutoGioBatDau.Value,
                                GioKetThuc = dto.AutoGioKetThuc.Value,
                                PhongHocId = dto.AutoPhongHocId.Value
                            });
                            schedulesCreated++;
                        }
                        else
                        {
                            conflictCount++;
                        }
                    }
                    currentDate = currentDate.AddDays(1);
                }
                await _context.SaveChangesAsync(); // Lưu các lịch học đã tạo
            }

            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }
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
            string scheduleMessage = schedulesCreated > 0
        ? $"Tạo thành công {schedulesCreated} lịch học."
        : "Không tạo lịch học tự động.";
            if (conflictCount > 0)
            {
                scheduleMessage += $" ({conflictCount} lịch bị bỏ qua do trùng lịch).";
            }

            return CreatedAtAction(nameof(GetLopHocPhans), new { id = newLopHP.Id }, new
            {
                result = true,
                code = 201,
                message = $"Tạo Lớp Học Phần {newLopHP.MaLopHocPhan} thành công. {scheduleMessage}",
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

        // API 6: Tạo nhiều Lịch học cho một Lớp Học Phần
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        // Endpoint nhận MỘT DANH SÁCH các LichHocDTO
        [HttpPost("lophocphan/lichhoc/batch")]
        public async Task<IActionResult> CreateBatchLichHoc([FromBody] List<LichHocDTO> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(new { result = false, message = "Danh sách lịch học rỗng." });
            }

            var lopHocPhanId = dtos.First().LopHocPhanId;
            var lhp = await _context.LopHocPhans
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lopHocPhanId);

            if (lhp == null)
                return NotFound(new { result = false, message = $"Không tìm thấy Lớp Học Phần với Id = {lopHocPhanId}" });

            int giangVienId = (int)lhp.GiangVienId;
            var newLichHocs = new List<LichHoc>();
            int conflictCount = 0;

            // 1. Lặp qua danh sách DTO để kiểm tra trùng lặp và tạo đối tượng
            foreach (var dto in dtos)
            {
                // Đảm bảo tất cả DTO đều thuộc cùng một LHP (kiểm tra an toàn)
                if (dto.LopHocPhanId != lopHocPhanId) continue;

                // **Quan trọng:** Kiểm tra trùng lịch trước khi tạo

                // Kiểm tra trùng lịch Giảng viên
                var isGVConflict = await IsGiangVienConflict(giangVienId, dto.Ngay, dto.GioBatDau, dto.GioKetThuc);
                if (isGVConflict)
                {
                    conflictCount++;
                    continue; // Bỏ qua lịch học này
                }

                // Kiểm tra trùng lịch Phòng
                var isRoomConflict = await IsPhongHocConflict(dto.PhongHocId, dto.Ngay, dto.GioBatDau, dto.GioKetThuc);
                if (isRoomConflict)
                {
                    conflictCount++;
                    continue; // Bỏ qua lịch học này
                }

                // Nếu không trùng, thêm vào danh sách để Save
                newLichHocs.Add(new LichHoc
                {
                    LopHocPhanId = dto.LopHocPhanId,
                    Ngay = dto.Ngay.Date,
                    GioBatDau = dto.GioBatDau,
                    GioKetThuc = dto.GioKetThuc,
                    PhongHocId = dto.PhongHocId
                });
            }

            if (!newLichHocs.Any())
            {
                return BadRequest(new { result = false, message = $"Không có lịch học nào được tạo do {conflictCount} lịch bị trùng hoặc danh sách rỗng." });
            }

            // 2. Thêm tất cả lịch học không bị trùng vào DB
            _context.LichHocs.AddRange(newLichHocs);
            await _context.SaveChangesAsync();

            string successMessage = $"Tạo thành công {newLichHocs.Count} lịch học.";
            if (conflictCount > 0)
            {
                successMessage += $" ({conflictCount} lịch đã bị bỏ qua do trùng lịch Giảng viên hoặc Phòng học)";
            }

            return Ok(new
            {
                result = true,
                code = 200,
                message = successMessage
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
            var lhpList = await _context.LopHocPhans
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .Include(l => l.TrangThai)
                .Include(l => l.HocKy)   // 🔥 Thêm include học kỳ
                .Select(l => new
                {
                    l.Id,
                    l.MaLopHocPhan,
                    TenLop = l.TenLopHocPhan,

                    MonHoc = l.MonHoc.TenMonHoc,
                    GiangVien = l.GiangVien.HoVaTenDem + " " + l.GiangVien.Ten,

                    // 🔥 Học kỳ
                    HocKyId = l.HocKyId,
                    TenHocKy = l.HocKy.TenHocKy,
                    NgayBatDauHocKy = l.HocKy.NgayBatDau,
                    NgayKetThucHocKy = l.HocKy.NgayKetThuc,

                    SiSoToiDa = l.SiSo,
                    SiSoThucTe = _context.DangKyHocPhans.Count(dk => dk.LopHocPhanId == l.Id),

                    TrangThai = l.TrangThai.TenTrangThai,

                    LichHoc = _context.LichHocs
                        .Where(lh => lh.LopHocPhanId == l.Id)
                        .Select(lh => new
                        {
                            lh.Ngay,
                            lh.GioBatDau,
                            lh.GioKetThuc,
                            Phong = lh.PhongHocId.ToString()
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

            return await _context.LichHocs
                .AnyAsync(l =>
                    l.LopHocPhan.GiangVienId == giangVienId &&
                    l.Ngay.Date == ngay.Date &&
                    l.GioBatDau < gioKetThuc &&
                    l.GioKetThuc > gioBatDau
                );
        }
        // Kiểm tra Phòng học có trùng lịch không
        private async Task<bool> IsPhongHocConflict(int phongHocId, DateTime ngay, TimeSpan gioBatDau, TimeSpan gioKetThuc)
        {
            var lichPhong = await _context.LichHocs
                .Where(l => l.PhongHocId == phongHocId)
                .AsNoTracking()
                .ToListAsync();

            return await _context.LichHocs
                .AnyAsync(l =>
                    l.PhongHocId == phongHocId &&
                    l.Ngay.Date == ngay.Date &&
                    l.GioBatDau < gioKetThuc &&
                    l.GioKetThuc > gioBatDau
                );
        }

        //// --- CHỨC NĂNG 4: AUTO ĐĂNG KÝ HỌC PHẦN BẮT BUỘC ---

        //// API: Kích hoạt Job tự động đăng ký HP bắt buộc
        //[Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[HttpPost("auto-dangky-batbuoc/{hocKyId}")]
        //public async Task<IActionResult> RunAutoDangKyBatBuoc(int hocKyId, [FromQuery] int thuTuHocKy)
        //{
        //    // 1. Lấy thông tin học kỳ thực tế
        //    var hocKy = await _context.HocKys.FindAsync(hocKyId);
        //    if (hocKy == null)
        //        return NotFound(new { result = false, message = "Không tìm thấy Học kỳ." });

        //    int successCount = 0;
        //    int errorCount = 0;

        //    // 2. Lấy danh sách sinh viên đang học
        //    var sinhViens = await _context.SinhViens
        //        .Include(sv => sv.Lop)
        //        .Where(sv => sv.TrangThai.TenTrangThai == "Đang học")
        //        .ToListAsync();

        //    foreach (var sv in sinhViens)
        //    {
        //        if (sv.Lop?.NganhId == null)
        //            continue; // bỏ qua sinh viên chưa có ngành

        //        // 3. Lấy danh sách môn bắt buộc theo thứ tự học kỳ và ngành
        //        var ctBatBuoc = await _context.ChiTietChuongTrinhDaoTaos
        //            .Where(ct => ct.BatBuoc &&
        //                         ct.HocKy == thuTuHocKy &&
        //                         ct.ChuongTrinhDaoTao.NganhHocId == sv.Lop.NganhId)
        //            .ToListAsync();

        //        foreach (var monCt in ctBatBuoc)
        //        {
        //            // 4. Tìm lớp học phần còn chỗ
        //            var lhpTuongUng = await _context.LopHocPhans
        //                .Where(l => l.MonHoc.MaMonHoc == monCt.MaMonHoc &&
        //                            l.HocKyId == hocKyId &&
        //                            l.TrangThai.TenTrangThai == "Đang mở" &&
        //                            l.SiSo > _context.DangKyHocPhans.Count(dk => dk.LopHocPhanId == l.Id))
        //                .FirstOrDefaultAsync();

        //            if (lhpTuongUng != null)
        //            {
        //                // 5. Kiểm tra sinh viên đã đăng ký chưa
        //                var isRegistered = await _context.DangKyHocPhans
        //                    .AnyAsync(dk => dk.SinhVienId == sv.Id && dk.LopHocPhanId == lhpTuongUng.Id);

        //                if (!isRegistered)
        //                {
        //                    _context.DangKyHocPhans.Add(new DangKyHocPhan
        //                    {
        //                        SinhVienId = sv.Id,
        //                        LopHocPhanId = lhpTuongUng.Id,
        //                        NgayDangKy = DateTime.Now,
        //                        LoaiDangKy = "BatBuocAuto"
        //                    });
        //                    successCount++;
        //                }
        //            }
        //            else
        //            {
        //                errorCount++;
        //            }
        //        }
        //    }

        //    // 6. Lưu thay đổi
        //    await _context.SaveChangesAsync();

        //    // 7. Ghi log hoạt động
        //    var userId = User.FindFirst("userId")?.Value;
        //    var userName = User.FindFirst("username")?.Value ?? "Unknown";

        //    if (string.IsNullOrEmpty(userId))
        //        return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });

        //    await _activityLogService.LogAsync(
        //        userId: userId,
        //        userName: userName,
        //        device: Request.Headers["User-Agent"].ToString(),
        //        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
        //        actionType: "JOB_RUN",
        //        tableName: "DangKyHocPhans",
        //        objectId: hocKyId.ToString(),
        //        description: $"Chạy Job Auto Đăng ký HP Bắt buộc cho Học kỳ {hocKy.TenHocKy} " +
        //                     $"(ThuTuHocKy={thuTuHocKy}). Thành công: {successCount}, Lỗi: {errorCount}"
        //    );

        //    // 8. Trả kết quả
        //    return Ok(new
        //    {
        //        result = true,
        //        code = 200,
        //        message = $"Job Auto Đăng ký HP Bắt buộc cho Học kỳ {hocKy.TenHocKy} (ThuTuHocKy={thuTuHocKy}) đã hoàn thành. " +
        //                  $"Thành công: {successCount} đăng ký, Lỗi: {errorCount} môn không tìm thấy lớp."
        //    });
        //}

        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("auto-create-enroll")]
        public async Task<IActionResult> AutoCreateClassAndEnroll([FromBody] AutoEnrollmentApiPayload payload)
        {
            // 1. Validate Dữ liệu đầu vào
            if (!ModelState.IsValid)
            {
                return BadRequest(new { result = false, message = "Dữ liệu đầu vào không hợp lệ.", errors = ModelState });
            }

            if (payload.MandatorySubjectCodes == null || !payload.MandatorySubjectCodes.Any())
            {
                return Ok(new
                {
                    result = true,
                    code = 200,
                    message = "Không có môn học bắt buộc nào được gửi. Bỏ qua tạo lớp và đăng ký.",
                    data = new { ClassesCreated = 0, EnrollmentsCreated = 0, SchedulesCreated = 0 }
                });
            }

            // 2. Gọi Service để chạy nghiệp vụ chính
            int classesCreated = 0;
            int enrollmentsCreated = 0;
            int schedulesCreated = 0;
            string logDesc = "";

            try
            {
                // Gọi hàm Service (đã được cập nhật để nhận AutoEnrollmentRequestWithScheduleDTO hoặc lớp kế thừa nó)
                // Lưu ý: AutoEnrollmentApiPayload kế thừa từ AutoEnrollmentRequestWithScheduleDTO nên truyền vào được.
                (int createdClassesCount, int successEnrollmentsCount, int schedulesCreatedCount) =
                    await _lopHocPhanService.RunAutoEnrollmentJobAsync(payload);

                classesCreated = createdClassesCount;
                enrollmentsCreated = successEnrollmentsCount;
                schedulesCreated = schedulesCreatedCount;

                logDesc = $"API Auto Tạo/Đăng ký cho Ngành {payload.NganhId}, Khóa {payload.KhoaNhapHoc}, Kỳ {payload.ThuTuHocKy}. " +
                          $"Kết quả: Tạo **{classesCreated}** LHP, **{schedulesCreated}** Lịch học, Đăng ký **{enrollmentsCreated}** lượt.";
            }
            catch (InvalidOperationException ex)
            {
                // Bắt các lỗi nghiệp vụ từ Service (ví dụ: Không tìm thấy Khóa học)
                return BadRequest(new { result = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Bắt lỗi chung không mong muốn
                logDesc = $"LỖI: API Auto Tạo/Đăng ký cho HK {payload.HocKyId}. Lỗi: {ex.Message}";
                // Có thể ghi log lỗi chi tiết ở đây nếu cần
                return StatusCode(500, new { result = false, message = "Lỗi máy chủ nội bộ khi chạy đăng ký tự động.", errorDetail = ex.Message });
            }

            // 3. Ghi Log Hoạt động (Chỉ khi có thao tác thực hiện)
            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value ?? "Unknown";

            if (!string.IsNullOrEmpty(logDesc) && (classesCreated > 0 || enrollmentsCreated > 0))
            {
                await _activityLogService.LogAsync(
                    userId: userId,
                    userName: userName,
                    device: Request.Headers["User-Agent"].ToString(),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                    actionType: "AUTO_CREATE_ENROLL",
                    tableName: "LopHocPhans, DangKyHocPhans",
                    objectId: payload.HocKyId.ToString(),
                    description: logDesc
                );
            }

            // 4. Trả kết quả thành công
            return Ok(new
            {
                result = true,
                code = 200,
                message = logDesc, // Trả về thông báo chi tiết
                data = new { ClassesCreated = classesCreated, EnrollmentsCreated = enrollmentsCreated, SchedulesCreated = schedulesCreated }
            });
        }

        // API MỚI 2: Tạo Đợt Đăng ký mới cho các LHP đã chọn
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("dotdangky-lhp")]
        public async Task<IActionResult> CreateDotDangKyForLhps([FromBody] QuanLyDangKyLHPRequestDTO dto)
        {
            // ... (Kiểm tra ModelState và logic NgayBatDau/NgayKetThuc giữ nguyên)

            if (!ModelState.IsValid)
            {
                return BadRequest(new { result = false, message = "Dữ liệu đầu vào không hợp lệ.", errors = ModelState });
            }

            if (dto.NgayBatDau >= dto.NgayKetThuc)
            {
                return BadRequest(new { result = false, message = "Ngày bắt đầu phải trước ngày kết thúc." });
            }

            if (dto.LopHocPhanIds == null || !dto.LopHocPhanIds.Any())
            {
                return BadRequest(new { result = false, message = "Phải chọn ít nhất một Lớp Học Phần." });
            }

            // 1. Lấy thông tin người dùng cho Log
            var userId = User.FindFirst("userId")?.Value;
            var userName = User.FindFirst("username")?.Value ?? "Unknown";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { result = false, message = "Không xác định được người dùng từ token." });
            }

            // 2. Thực hiện nghiệp vụ tạo DotDangKy qua Service
            var (success, message) = await _lopHocPhanService.CreateDotDangKyForSelectedLhpsAsync(
                dto,
                userId,
                userName,
                Request.Headers["User-Agent"].ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A"
            );

            if (success)
            {
                return Ok(new { result = true, code = 201, message = message });
            }
            else
            {
                return BadRequest(new { result = false, message = message });
            }
        }
        [Authorize(Roles = SD.Role_Admin, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("lhp-for-reg")]
        public async Task<IActionResult> GetLopHocPhansForRegistration()
        {
            // 1. Lấy trạng thái "Đang mở"
            var trangThaiDangMoId = await _context.TrangThais
                .Where(t => t.LoaiTrangThai == "LopHocPhan" && t.TenTrangThai == "Chờ mở")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (trangThaiDangMoId == 0)
            {
                return NotFound(new { result = false, message = "Lỗi cấu hình: Không tìm thấy trạng thái 'Chờ mở' cho Lớp Học Phần." });
            }

            // 2. Lấy danh sách LHP: Đang mở VÀ Sĩ số thực tế < Sĩ số tối đa
            var lhpList = await _context.LopHocPhans
                .Where(l => l.TrangThaiId == trangThaiDangMoId)
                .Include(l => l.MonHoc)
                .Include(l => l.GiangVien)
                .Include(l => l.HocKy)
                .Select(l => new
                {
                    l.Id,
                    l.MaLopHocPhan,
                    TenLop = l.TenLopHocPhan,
                    MonHoc = l.MonHoc.TenMonHoc,
                    MaGiangVien = l.GiangVien.MaGiangVien,
                    GiangVien = (l.GiangVien.HoVaTenDem + " " + l.GiangVien.Ten) ?? "Chưa gán GV",
                    HocKyId = l.HocKyId,
                    TenHocKy = l.HocKy.TenHocKy,
                    SiSoToiDa = l.SiSo,
                    SiSoThucTe = _context.DangKyHocPhans.Count(dk => dk.LopHocPhanId == l.Id)
                })
                .Where(l => l.SiSoThucTe < l.SiSoToiDa) // Lọc LHP còn chỗ
                .OrderBy(l => l.MonHoc)
                .ToListAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                message = $"Lấy danh sách {lhpList.Count} LHP còn chỗ thành công.",
                data = lhpList
            });
        }


        // POST: api/admin/CheckPhongVaGan/{donId}
        [HttpPost("CheckPhongVaGan/{donId}")]
        public async Task<IActionResult> CheckPhongVaGan(int donId)
        {
            var don = await _context.XinVangDays
                        .Include(x => x.PhongHoc) // load navigation
                        .FirstOrDefaultAsync(x => x.Id == donId);

            if (don == null)
                return NotFound(new { result = false, message = "Đơn không tồn tại" });

            if (don.PhongHoc != null && !string.IsNullOrEmpty(don.PhongHoc.MaPhongHoc))
                return BadRequest(new { result = false, message = "Đơn đã có phòng" });

            var dsPhong = await _context.PhongHocs.ToListAsync();

            foreach (var phong in dsPhong)
            {
                bool trungLich = await _context.XinVangDays
                    .AnyAsync(x => x.NgayDayBu == don.NgayDayBu
                                   && x.PhongHocId == phong.Id
                                   && ((x.GioBatDauDayBu < don.GioKetThucDayBu)
                                       && (don.GioBatDauDayBu < x.GioKetThucDayBu)));

                if (!trungLich)
                {
                    don.PhongHoc = phong; // hoặc don.PhongId = phong.Id;
                    await _context.SaveChangesAsync();
                    return Ok(new { result = true, phong = phong.MaPhongHoc });
                }
            }

            return BadRequest(new { result = false, message = "Không còn phòng trống" });
        }


        // POST: api/admin/DuyetDon/{donId}
        [HttpPost("DuyetDon/{donId}")]
        public async Task<IActionResult> DuyetDon(int donId)
        {
            var don = await _context.XinVangDays
                .Include(x => x.LichHoc)
                .Include(x => x.GiangVien)
                    .ThenInclude(us => us.User)
                .Include(x => x.PhongHoc)
                .FirstOrDefaultAsync(x => x.Id == donId);

            if (don == null)
                return NotFound(new { result = false, message = "Đơn không tồn tại" });

            // Xóa lịch cũ nếu có
            if (don.LichHoc != null)
            {
                _context.LichHocs.Remove(don.LichHoc);
                don.LichHoc = null;
                don.LichHocId = null; // quan trọng để EF không lỗi
            }

            // Tạo lịch mới dựa vào NgayDayBu, GioBatDauDayBu...
            var lichMoi = new LichHoc
            {
                LopHocPhanId = don.LopHocPhanId,
                Ngay = don.NgayDayBu.Value,
                GioBatDau = don.GioBatDauDayBu.Value,
                GioKetThuc = don.GioKetThucDayBu.Value,
                PhongHocId = don.PhongHocId
            };
            _context.LichHocs.Add(lichMoi);
            don.LichHoc = lichMoi;

            // Cập nhật trạng thái
            var trangThaiDuyet = await _context.TrangThais
                .FirstOrDefaultAsync(t => t.LoaiTrangThai == "DonPhieu" && t.TenTrangThai == "Đã duyệt");
            don.TrangThai = trangThaiDuyet;

            await _context.SaveChangesAsync();

            var emailGV = don.GiangVien?.User?.Email;

            var gioBatDauBu = don.GioBatDauDayBu?.ToString(@"hh\:mm") ?? "-";
            var gioKetThucBu = don.GioKetThucDayBu?.ToString(@"hh\:mm") ?? "-";

            await _emailSender.SendEmailAsync(
                emailGV,
                "Thông báo duyệt đơn xin vắng dạy",
                $"Đơn xin vắng dạy của bạn vào ngày {don.NgayXinVang:dd/MM/yyyy} đã được duyệt.\n" +
                $"Thông tin cụ thể:\n" +
                $"- Ngày xin vắng: {don.NgayXinVang} ({don.CaXinVang})\n" +
                $"- Ngày dạy bù: {don.NgayDayBu:dd/MM/yyyy} ({gioBatDauBu} - {gioKetThucBu})\n" +
                $"- Phòng học: {don.PhongHoc?.MaPhongHoc}"
            );


            return Ok(new { result = true });
        }




        // POST: api/admin/TuChoiDon/{donId}
        [HttpPost("TuChoiDon/{donId}")]
        public async Task<IActionResult> TuChoiDon(int donId)
        {
            var don = await _context.XinVangDays.FirstOrDefaultAsync(x => x.Id == donId);
            if (don == null) return NotFound(new { result = false, message = "Đơn không tồn tại" });

            var trangThaiTuChoi = await _context.TrangThais
                                        .FirstOrDefaultAsync(t => t.LoaiTrangThai == "DonPhieu" && t.TenTrangThai == "Từ chối");

            don.TrangThai = trangThaiTuChoi;
            await _context.SaveChangesAsync();
            return Ok(new { result = true });
        }
    }
}