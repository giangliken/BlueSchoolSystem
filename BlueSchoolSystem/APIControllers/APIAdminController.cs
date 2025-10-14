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
    public class APIAdminController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly ApplicationDbContext _context;
        public APIAdminController(IActivityLogService activityLogService, ApplicationDbContext context)
        {
            _activityLogService = activityLogService;
            _context = context;
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
    }
}
