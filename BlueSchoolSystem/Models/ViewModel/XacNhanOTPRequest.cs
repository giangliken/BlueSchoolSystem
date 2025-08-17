using Microsoft.AspNetCore.Mvc;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class XacNhanOTPRequest
    {
        public string Email { get; set; }
        public string OTPCode { get; set; }
    }
}
