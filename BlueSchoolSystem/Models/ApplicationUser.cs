using Microsoft.AspNetCore.Identity;

namespace BlueSchoolSystem.Models
{
    public class ApplicationUser: IdentityUser
    {
        public virtual SinhVien Student { get; set; }
    }
}
