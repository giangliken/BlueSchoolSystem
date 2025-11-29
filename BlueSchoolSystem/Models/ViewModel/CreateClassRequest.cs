using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class CreateClassRequest
    {
        [Required(ErrorMessage = "Mã lớp là bắt buộc.")]
        [StringLength(20, MinimumLength = 2, ErrorMessage = "Mã lớp phải từ 2 đến 20 ký tự.")]
        public string MaLop { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên lớp là bắt buộc.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Tên lớp phải từ 2 đến 50 ký tự.")]
        public string TenLop { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Ngành học là bắt buộc.")]
        public int NganhId { get; set; }
    }
}
