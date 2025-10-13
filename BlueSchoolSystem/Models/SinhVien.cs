using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BlueSchoolSystem.Models
{
    public class SinhVien
    {
        public int Id { get; set; }
        [RegularExpression("^[0-9]+$", ErrorMessage = "MSSV chỉ được chứa số")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "MSSV phải từ 10 đến 15 ký tự")]

        [Required(ErrorMessage = "MSSV là bắt buộc")]
        public string MSSV { get; set; } // Mã số sinh viên

        [Required(ErrorMessage = "Họ và tên đệm là bắt buộc")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Họ và tên đệm phải ít nhất 5 ký tự và tối đa 50 ký tự")]
        public string HoVaTenDem { get; set; }

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Tên phải từ 1 đến 20 ký tự")]
        public string Ten { get; set; } // Tên

        [Required(ErrorMessage = "CCCD là bắt buộc")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "CCCD phải đúng 12 số")]
        public string CCCD { get; set; } // Số căn cước công dân

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; } // Ngày sinh
        public bool GioiTinh { get; set; } // Giới tính

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(200, ErrorMessage = "Địa chỉ tối đa 200 ký tự")]
        public string DiaChi { get; set; } // Địa chỉ

        public int? LopId { get; set; }
        public LopHoc? Lop { get; set; }

        [Required(ErrorMessage = "Ngày nhập học là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime NgayNhapHoc { get; set; } // Ngày nhập học

        [Required(ErrorMessage = "Ngày tốt nghiệp là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime NgayTotNghiep { get; set; } // Ngày tốt nghiệp
        public int TrangThaiId { get; set; } // Trạng thái (đang học, đã tốt nghiệp, bỏ học, v.v.)
        public TrangThai? TrangThai { get; set; } // Liên kết với bảng trạng thái
        public string? GhiChu { get; set; } // Ghi chú thêm về sinh viên
        public string? AvatarUrl { get; set; } // URL của ảnh đại diện sinh viên
        public string? UserId { get; set; } // ID của người dùng liên kết với sinh viên
        [JsonIgnore]

        public ApplicationUser? User { get; set; } // Liên kết với ApplicationUser để quản lý thông tin người dùng
        public DateTime CreatedAt { get; set; } // Ngày tạo bản ghi
        public DateTime UpdatedAt { get; set; } // Ngày cập nhật bản ghi
        public SinhVien()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        public ICollection<BangDiem> BangDiems { get; set; } = new List<BangDiem>();

        public List<ChiTietDiemDanh> ChiTietDiemDanhs { get; set; } = new();

    }
}
