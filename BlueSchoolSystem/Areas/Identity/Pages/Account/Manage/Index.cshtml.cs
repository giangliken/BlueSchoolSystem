// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BlueSchoolSystem.Models;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BlueSchoolSystem.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ApplicationDbContext context    )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }
        public string FullName { get; set; }
        public string NgaySinh { get; set; }
        public string Lop { get; set; }
        public string NganhHoc { get; set; }
        public string KhoaVien { get; set; }
        public bool IsEmailConfirmed { get; set; }


        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

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
            [Phone]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [EmailAddress]
            [Display(Name ="Email")]
            public string Email { get; set; }
            [Display(Name = "Ảnh đại diện")]
            public IFormFile? AvatarImage { get; set; }

            public string? AvatarUrl { get; set; }

        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            Username = userName;
            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            string avatarUrl = null;

            if (user.SinhViens != null)
            {
                FullName = user.SinhViens.HoVaTenDem + " " + user.SinhViens.Ten;
                NgaySinh = user.SinhViens.NgaySinh.ToString("dd/MM/yyyy");
                Lop = user.SinhViens.Lop?.TenLop ?? "Chưa đăng ký lớp";
                NganhHoc = user.SinhViens.Lop?.Nganh?.TenNganh ?? "Chưa đăng ký ngành học";
                KhoaVien = user.SinhViens.Lop?.Nganh?.Khoa?.TenKhoa ?? "Chưa thuộc khoa viện nào";
                avatarUrl = user.SinhViens.AvatarUrl;

            }
            else if (user.GiangViens != null)
            {
                FullName = user.GiangViens.HoVaTenDem + " " + user.SinhViens.Ten;
                avatarUrl = user.GiangViens.AvatarUrl;
            }
            else if (await _userManager.IsInRoleAsync(user, SD.Role_Admin))
            {
                FullName = "Tài khoản quản trị";
                //avatarUrl = "/images/admin-avatar.png";
            }
            else
            {
                FullName = "Không xác định";
                //avatarUrl = "/images/default-avatar.png";
            }

                Input = new InputModel
                {
                    PhoneNumber = phoneNumber,
                    Email = email,
                    AvatarUrl = avatarUrl
                };
        }



        public async Task<IActionResult> OnGetAsync()
        {
            var user = await GetUserWithRelationsAsync();
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            await LoadAsync(user);
            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            var user = await GetUserWithRelationsAsync();
            if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Lỗi không xác định khi cập nhật số điện thoại.";
                    return RedirectToPage();
                }
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.Email != currentEmail)
            {
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.Email);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page("/Account/ConfirmEmailChange", null,
                    new { area = "Identity", userId = await _userManager.GetUserIdAsync(user), email = Input.Email, code },
                    Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Xác nhận thay đổi email",
                    $"Vui lòng xác nhận email mới bằng cách <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>bấm vào đây</a>.");

                StatusMessage = "Email xác nhận đã được gửi. Vui lòng kiểm tra hộp thư.";
                return RedirectToPage();
            }
            //Nếu người dùng upload ảnh mới
            if (Input.AvatarImage != null && Input.AvatarImage.Length > 0)
            {
                var avatarUrl = await SaveImage(Input.AvatarImage, user.UserName);

                if (user.SinhViens != null)
                {
                    user.SinhViens.AvatarUrl = avatarUrl;
                    _context.Update(user.SinhViens);
                }
                else if (user.GiangViens != null)
                {
                    user.GiangViens.AvatarUrl = avatarUrl;
                    _context.Update(user.GiangViens);
                }

                await _context.SaveChangesAsync();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thông tin cá nhân đã được cập nhật.";
            if (user.SinhViens != null)
            {
                user.SinhViens.UpdatedAt = DateTime.Now;
            }
            else if (user.GiangViens != null)
            {
                user.GiangViens.UpdatedAt = DateTime.Now;
            }
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        private async Task<string> SaveImage(IFormFile image, string us)
        {
            var folderPath = Path.Combine("wwwroot", "assets", "images", "avatar");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Đặt tên file mới để tránh trùng lặp, ví dụ: userId + đuôi file gốc
            var extension = Path.GetExtension(image.FileName);
            var newFileName = $"{us}{extension}";
            var savePath = Path.Combine(folderPath, newFileName);

            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Trả về URL đúng để hiển thị ảnh
            return $"/assets/images/avatar/{newFileName}";
        }

        private Task<ApplicationUser> GetUserWithRelationsAsync()
        {
            var userId = _userManager.GetUserId(User);
            return _userManager.Users
                .Include(u => u.SinhViens)
                .Include(u => u.GiangViens)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

    }
}
