using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class NganhHoc
    {

        public int Id { get; set; } // Primary Key, có thể là Id tự động tăng

        [Required(ErrorMessage ="Mã ngành là bắt buộc")]
        public string MaNganh { get; set; }

        [Required(ErrorMessage ="Tên ngành là bắt buộc")]
        public string TenNganh { get; set; }

        // FK về Khoa (nếu muốn mở rộng)
        [Required(ErrorMessage ="Khoa/Viện là bắt buộc")]
        public int KhoaId { get; set; }
        public Khoa? Khoa { get; set; }
        public ICollection<MonHoc>? MonHocs { get; set; }


    }
}
