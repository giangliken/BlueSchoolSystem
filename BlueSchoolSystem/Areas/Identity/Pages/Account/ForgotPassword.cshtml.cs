// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlueSchoolSystem.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly ApiSettings _apiSettings;
        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, ApplicationDbContext context, IOptions<ApiSettings> apiSettings)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
            _apiSettings = apiSettings.Value;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage ="Email là bắt buộc")]
            //[EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Email không hợp lệ")]

            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_apiSettings.BaseUrl);
                var requestData = new
                {
                    Email = Input.Email
                };

                var response = await client.PostAsJsonAsync("api/gui-otp", requestData);

                if (response.IsSuccessStatusCode)
                {
                    // Thành công -> chuyển qua trang nhập OTP
                    TempData["Email"] = Input.Email;
                    return RedirectToPage("./EnterOTP", new { email = Input.Email });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Nếu API trả về lỗi nhưng không phải "email không tồn tại" → vẫn qua trang nhập OTP
                    if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        TempData["Email"] = Input.Email;
                        Console.WriteLine($"Cảnh báo từ API: {errorContent}");
                        return RedirectToPage("./EnterOTP", new { email = Input.Email });
                    }

                    // Email không tồn tại → parse JSON để lấy message
                    var errorJson = JsonDocument.Parse(errorContent);
                    if (errorJson.RootElement.TryGetProperty("message", out var messageProp))
                    {
                        ModelState.AddModelError(string.Empty, messageProp.GetString());
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại.");
                    }
                    return Page();
                }
            }
        }




    }
}
