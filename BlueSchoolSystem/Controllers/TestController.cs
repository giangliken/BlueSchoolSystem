using Microsoft.AspNetCore.Mvc;
using BlueSchoolSystem.Repository;

namespace BlueSchoolSystem.Controllers
{
    public class TestController : Controller
    {
        private readonly IEmailSender _emailSender;

        public TestController(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        [HttpGet("/test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            await _emailSender.SendEmailAsync(
                "nguyentruonggiang.soctrang.2004@gmail.com",
                "Test Email từ BlueSchoolSystem",
                "<b>Hello! Đây là email test thành công.</b>"
            );

            return Ok("Đã gửi thử email!");
        }
    }
}
