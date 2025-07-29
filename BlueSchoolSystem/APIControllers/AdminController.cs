using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        public AdminController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
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
