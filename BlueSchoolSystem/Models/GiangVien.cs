namespace BlueSchoolSystem.Models
{
    public class GiangVien
    {
        public int Id { get; set; }
        public string MaGiangVien { get; set; } // Mã giảng viên
        public string HoVaTenDem { get; set; } // Họ và tên đệm
        public string Ten { get; set; } // Tên
        public string CCCD { get; set; } // Số căn cước công dân
        public DateTime NgaySinh { get; set; } // Ngày sinh
        public bool GioiTinh { get; set; } // Giới tính
        public string DiaChi { get; set; } // Địa chỉ

        public int TrangThaiId { get; set; } // Trạng thái
        public TrangThai? TrangThai { get; set; } // Liên kết với bảng trạng thái
        public string? GhiChu { get; set; } // Ghi chú thêm về giảng viên
        public string? AvatarUrl { get; set; } // URL của ảnh đại diện giảng viên

        public int KhoaId { get; set; }
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
