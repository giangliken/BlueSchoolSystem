using BlueSchoolSystem.Models;
using BlueSchoolSystem.Models.ViewModel;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NuGet.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlueSchoolSystem.APIControllers
{
    [Route("api")]
    [ApiController]
    public class APIAccountController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        public APIAccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IOptions<JwtSettings> jwtSettings,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
            _emailSender = emailSender;
            _context = context;
        }

        [EnableRateLimiting("LoginLimiter")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Models.ViewModel.LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");

            //var user = await _userManager.FindByEmailAsync(model.Email);
            var user = await _userManager.Users
                .Include(u => u.SinhViens)
                .Include(u => u.GiangViens)
                .FirstOrDefaultAsync(u => u.UserName == model.UserName);

            if (user == null)
                return Unauthorized(new 
                {   
                    result = false,
                    code = 401,
                    message = "Tên đăng nhập không tồn tại" 
                });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Unknown";
                var token = GenerateJwtToken(user, roles);
                HttpContext.Session.SetString("access_token", token);

                // Tạo refresh token mới
                string refreshTokenValue = Guid.NewGuid().ToString("N");
                var refreshToken = new RefreshToken
                {
                    Token = refreshTokenValue,
                    UserId = user.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7), // hạn 7 ngày
                    IsRevoked = false
                };
                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();


                //Nếu người dùng đăng nhập bằng tài khoản Admin
                if (role == SD.Role_Admin)
                {
                    return Ok(new
                    {
                        result = true,
                        code = 200,
                        message = "Đăng nhập thành công",
                        token = token,
                        refresh_token = refreshTokenValue,

                        user = new
                        {
                            username = user.UserName,
                            role = role,
                            email = user.Email,
                        }
                    });

                }
                
                //Nếu người dùng đăng nhập bằng tài khoản sinh viên
                if (role == SD.Role_Student)
                {
                    return Ok(new
                    {
                        result = true,
                        code = 200,
                        message = "Đăng nhập thành công",
                        token = token,
                        refresh_token = refreshTokenValue,

                        user = new
                        {
                            username = user.UserName,
                            role = role,
                            mssv = user.SinhViens.MSSV,
                            hoSv = user.SinhViens.HoVaTenDem,
                            tenSv = user.SinhViens.Ten,
                            email = user.Email,
                            ngaySinh = user.SinhViens.NgaySinh,
                        }
                    });
                }

                //Nếu người dùng đăng nhập bằng tài khoản giảng viên
                if (role == SD.Role_Teacher)
                {
                    return Ok(new
                    {
                        result = true,
                        code = 200,
                        message = "Đăng nhập thành công",
                        token = token,
                        refresh_token = refreshTokenValue,

                        user = new
                        {
                            username = user.UserName,
                            role = role,
                            maGV = user.GiangViens.MaGiangVien,
                            hoGV = user.GiangViens.HoVaTenDem,
                            tenGV = user.GiangViens.Ten,
                            email = user.Email,
                            ngaySinh = user.GiangViens.NgaySinh,
                        }
                    });
                }

            }

            return Unauthorized(new 
            { 
                result = false,
                code = 401,
                message = "Mật khẩu không đúng" 
            });
        }



        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                //new Claim(ClaimTypes.NameIdentifier, user.Id),
                //new Claim(ClaimTypes.Email, user.Email),
                //new Claim(ClaimTypes.Name, user.UserName)
                new("userId", user.Id),
                new("username", user.UserName),
                new("email", user.Email),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked && rt.ExpiryDate > DateTime.UtcNow);

            if (refreshToken == null)
                return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn" });

            // Tạo access token mới
            var user = refreshToken.User;
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = GenerateJwtToken(user, roles);

            // Cấp lại refresh token mới (hoặc dùng lại token cũ nếu muốn đơn giản)
            string newRefreshTokenValue = Guid.NewGuid().ToString("N");
            refreshToken.Token = newRefreshTokenValue;
            refreshToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                token = newAccessToken,
                refresh_token = newRefreshTokenValue
            });
        }


        //Gửi OTP khôi phục mật khẩu
        [HttpPost("gui-otp")]
        public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data.");
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Email này chưa liên kết với bất kì tài khoản nào trên hệ thống"
                });
            }
            //Kiểm tra xem đã có mã OTP chưa
            var existingOTP = await GetOTPByEmailAsync(request.Email);
            if (existingOTP != null)
            {
                //Nếu mã OTP đã tồn tại, kiểm tra xem nó có hết hạn không
                if (!IsOTPExpired(existingOTP.ExpireAt))
                {
                    return BadRequest(new
                    {
                        result = false,
                        code = 400,
                        message = $"Bạn đã gửi mã OTP trước đó. Bạn có thể gửi mã sau {existingOTP.ExpireAt}"
                    });
                }
                //Nếu mã OTP đã hết hạn, xóa nó
                _context.PasswordResetOTPs.Remove(existingOTP);
                await _context.SaveChangesAsync();
            }
            var otpCode = GenerateOTP();
            if (string.IsNullOrEmpty(otpCode))
            {
                return StatusCode(500, new
                {
                    result = false,
                    code = 500,
                    message = "Không thể tạo mã OTP"
                });
            }
            //Lưu mã OTP vào cơ sở dữ liệu
            await _context.PasswordResetOTPs.AddAsync(new PasswordResetOTP
            {
                Email = request.Email,
                OTPCode = otpCode,
                ExpireAt = DateTime.Now.AddMinutes(30)
            });

            _context.SaveChanges();

            //Gửi mã OTP qua email
            await _emailSender.SendEmailAsync(
                request.Email, 
                "Mã OTP khôi phục mật khẩu", 
                $"Mã OTP của bạn là: {otpCode}"
            );
            return Ok(new
            {
                result = true,
                code = 200,
                message = "Mã OTP đã được gửi đến email của bạn"
            });
        }


        //Hàm tạo mã OTP
        public string GenerateOTP()
        {
            Random random = new Random();
            int otp = random.Next(100000, 999999); // từ 100000 -> 999999
            return otp.ToString();
        }

        //Hàm kiểm tra thời gian hết hạn của mã OTP
        private bool IsOTPExpired(DateTime expireAt)
        {
            return DateTime.Now > expireAt;
        }

        //Hàm lấy ra mã OTP từ cơ sở dữ liệu
        private async Task<PasswordResetOTP> GetOTPByEmailAsync(string email)
        {
            return await _context.PasswordResetOTPs
                .Where(otp => otp.Email == email)
                .OrderByDescending(otp => otp.ExpireAt) // mới nhất
                .FirstOrDefaultAsync();
        }


        //hàm kiểm tra mã OTP
        [HttpPost("xac-nhan-otp")]
        public async Task<IActionResult> XacNhanOTP([FromBody] XacNhanOTPRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Dữ liệu không hợp lệ"
                });

            // Tìm mã OTP mới nhất của email này
            var existingOTP = await GetOTPByEmailAsync(request.Email);
            if (existingOTP == null)
            {
                return NotFound(new
                {
                    result = false,
                    code = 404,
                    message = "Không tìm thấy mã OTP cho email này"
                });
            }

            // Kiểm tra hết hạn
            if (IsOTPExpired(existingOTP.ExpireAt))
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Mã OTP đã hết hạn"
                });
            }

            // Kiểm tra khớp mã OTP
            if (existingOTP.OTPCode != request.OTPCode)
            {
                return BadRequest(new
                {
                    result = false,
                    code = 400,
                    message = "Mã OTP không đúng"
                });
            }

            // Nếu đúng OTP → có thể xóa OTP và cấp ResetToken

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return NotFound(new { result = false, code = 404, message = "Không tìm thấy user" });

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            _context.PasswordResetOTPs.Remove(existingOTP);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                result = true,
                code = 200,
                resetToken,
                message = "Xác thực OTP thành công. Bạn có thể đổi mật khẩu."
            });
        }

    }
}
