using Microsoft.AspNetCore.Identity;

namespace BlueSchoolSystem.Models
{
    public class ApplicationUser: IdentityUser
    {
        public virtual SinhVien? SinhViens { get; set; }
        public virtual GiangVien? GiangViens { get; set; }
    }
}
