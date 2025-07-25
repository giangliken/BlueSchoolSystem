using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class Class
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
        public Major? Nganh { get; set; }  

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
