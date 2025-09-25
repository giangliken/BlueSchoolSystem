using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class ApplicationUser: IdentityUser
    {

        public virtual SinhVien? SinhViens { get; set; }
        public virtual GiangVien? GiangViens { get; set; }
        [Required(ErrorMessage =("Số điện thoại là bắt buộc"))]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải có 10 số và bắt đầu bằng 0.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage =("Email là bắt buộc"))]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public override string Email { get; set; }
    }
}
