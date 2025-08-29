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
        public APIAdminController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
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
    }
}
