using Microsoft.AspNetCore.Identity;

namespace BlueSchoolSystem.Models
{
    public class ApplicationUser: IdentityUser
    {
        public virtual Student Student { get; set; }
    }
}
