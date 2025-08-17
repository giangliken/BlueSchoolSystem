using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace BlueSchoolSystem.Areas.Identity.Pages.Account
{
    public class EnterOTPModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiSettings _apiSettings;

        public EnterOTPModel(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IOptions<ApiSettings> apiSettings)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _apiSettings = apiSettings.Value;
        }

        // nhận từ query khi GET
        [BindProperty(SupportsGet = true)]
        public string Email { get; set; }

        [BindProperty]
        [Required, Display(Name = "Mã OTP")]
        [StringLength(6, MinimumLength = 6)]
        public string OTP { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(string.Empty, "Thiếu email.");
                return Page();
            }

            var now = DateTime.Now;
            var record = await _context.PasswordResetOTPs
                .FirstOrDefaultAsync(o => o.Email == Email && o.OTPCode == OTP);

            if (record == null || record.ExpireAt <= now)
            {
                ModelState.AddModelError(string.Empty, "Mã OTP không đúng hoặc đã hết hạn.");
                return Page();
            }

            // Gọi API xác nhận OTP để lấy resetToken
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiSettings.BaseUrl);

                var res = await client.PostAsJsonAsync("api/xac-nhan-otp", new
                {
                    Email = Email,
                    OTPCode = OTP
                });

                if (!res.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "Xác thực OTP thất bại, vui lòng thử lại.");
                    return Page();
                }

                var data = await res.Content.ReadFromJsonAsync<XacNhanOtpResponse>();
                if (data is null || !data.result || string.IsNullOrEmpty(data.resetToken))
                {
                    ModelState.AddModelError(string.Empty, data?.message ?? "Không lấy được reset token.");
                    return Page();
                }

                // API trả token "raw" có + / = → encode URL-safe trước khi gắn vào query
                var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(data.resetToken));

                // Redirect sang ResetPassword kèm email + tokenEncoded
                return RedirectToPage("./ResetPassword", new { email = Email, code = tokenEncoded });
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Có lỗi hệ thống khi xác thực OTP.");
                return Page();
            }
        }
    }

}
