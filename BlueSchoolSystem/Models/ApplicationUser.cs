using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class ApplicationUser: IdentityUser
    {

        public virtual SinhVien? SinhViens { get; set; }
        public virtual GiangVien? GiangViens { get; set; }
    }
}
