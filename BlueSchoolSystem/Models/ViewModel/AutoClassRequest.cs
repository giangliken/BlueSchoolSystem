using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class AutoClassRequest
    {
        [Required(ErrorMessage = "Khóa là bắt buộc.")]
        public int Khoa { get; set; }     // 2022
        [Required(ErrorMessage = "Ngành là bắt buộc.")]
        public int NganhId { get; set; }  // ngành đã chọn
        [Required(ErrorMessage = "Số lượng lớp là bắt buộc.")]
        public int SoLuong { get; set; }  // số lớp muốn tạo
    }
}
