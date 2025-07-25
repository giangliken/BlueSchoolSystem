using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class Faculty
    {
        public int Id { get; set; } // ID của khoa

        [StringLength(10, ErrorMessage = "Mã khoa không được vượt quá 10 ký tự")]
        [Display(Name = "Mã khoa")]
        public string MaKhoa { get; set; } // Mã khoa
        [StringLength(100, ErrorMessage = "Tên khoa không được vượt quá 100 ký tự")]
        [Display(Name = "Tên khoa")]
        public string TenKhoa { get; set; } // Tên khoa
        public string? MoTa { get; set; } // Mô tả về khoa

    }
}
