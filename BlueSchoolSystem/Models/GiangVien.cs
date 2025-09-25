using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class GiangVien
    {
        public int Id { get; set; }

        [Required(ErrorMessage =("Mã CV/GV là bắt buộc"))]
        public string MaGiangVien { get; set; } // Mã giảng viên

        [Required(ErrorMessage = "Họ và tên đệm là bắt buộc")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Họ và tên đệm phải ít nhất 5 ký tự và tối đa 50 ký tự")]
        
        public string HoVaTenDem { get; set; } // Họ và tên đệm

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Tên phải từ 1 đến 20 ký tự")]
        public string Ten { get; set; } // Tên

        [Required(ErrorMessage = "CCCD là bắt buộc")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải đúng 12 số")]
        public string CCCD { get; set; } // Số căn cước công dân

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public DateTime NgaySinh { get; set; } // Ngày sinh

        public bool GioiTinh { get; set; } // Giới tính

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        public string DiaChi { get; set; } // Địa chỉ

        public int TrangThaiId { get; set; } // Trạng thái
        public TrangThai? TrangThai { get; set; } // Liên kết với bảng trạng thái
        public string? GhiChu { get; set; } // Ghi chú thêm về giảng viên
        public string? AvatarUrl { get; set; } // URL của ảnh đại diện giảng viên

        [Required(ErrorMessage = "Nơi công tác là bắt buộc")]
        public int? KhoaId { get; set; }
        public Khoa? Khoa { get; set; }

        public ICollection<MonHoc>? MonHocs { get; set; } // Các môn giảng dạy
        public ICollection<LopHocPhan>? LopHocPhans { get; set; } // Các lớp học phần

        public string? UserId { get; set; } // ID của người dùng liên kết với giảng viên

        public ApplicationUser? User { get; set; } // Liên kết với ApplicationUser để quản lý thông tin người dùng
        public DateTime CreatedAt { get; set; } // Ngày tạo bản ghi
        public DateTime UpdatedAt { get; set; } // Ngày cập nhật bản ghi
        public GiangVien()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
