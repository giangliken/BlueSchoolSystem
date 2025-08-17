using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string NewPassword { get; set; }
    }
}
