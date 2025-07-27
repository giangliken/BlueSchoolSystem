using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlueSchoolSystem.Models
{
    public class LopHoc
    {
        public int Id { get; set; }

        [StringLength(10, ErrorMessage = "Mã lớp không được vượt quá 10 ký tự")]
        [Display(Name = "Mã lớp")]
        public string MaLop { get; set; }  

        [StringLength(100, ErrorMessage = "Tên lớp không được vượt quá 100 ký tự")]
        [Display(Name = "Tên lớp")]
        public string TenLop { get; set; }

        public string? MoTa { get; set; }  

        public int NganhId { get; set; }  
        public NganhHoc? Nganh { get; set; }
        
        [JsonIgnore]

        public ICollection<SinhVien> Students { get; set; } = new List<SinhVien>();
    }
}
