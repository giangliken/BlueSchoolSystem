using System.Text.Json.Serialization;

namespace BlueSchoolSystem.Models
{
    public class SinhVien
    {
        public int Id { get; set; }
        public string MSSV { get; set; } // Mã số sinh viên
        public string HoVaTenDem { get; set; } // Họ và tên đệm
        public string Ten { get; set; } // Tên
        public string CCCD { get; set; } // Số căn cước công dân
        public DateTime NgaySinh { get; set; } // Ngày sinh
        public bool GioiTinh { get; set; } // Giới tính
        public string DiaChi { get; set; } // Địa chỉ

        public int LopId { get; set; }
        public LopHoc? Lop { get; set; }

        public DateTime NgayNhapHoc { get; set; } // Ngày nhập học
        public DateTime NgayTotNghiep { get; set; } // Ngày tốt nghiệp
        public string TrangThai { get; set; } // Trạng thái (đang học, đã tốt nghiệp, bỏ học, v.v.)
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
    }
}
