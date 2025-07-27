namespace BlueSchoolSystem.Models
{
    public class GiangVien
    {
        public int Id { get; set; }
        public string MaGiangVien { get; set; } // Mã số sinh viên
        public string HoVaTenDem { get; set; } // Họ và tên đệm
        public string Ten { get; set; } // Tên
        public DateTime NgaySinh { get; set; } // Ngày sinh
        public bool GioiTinh { get; set; } // Giới tính
        public string DiaChi { get; set; } // Địa chỉ

        public string TrangThai { get; set; } // Trạng thái (đang học, đã tốt nghiệp, bỏ học, v.v.)
        public string? GhiChu { get; set; } // Ghi chú thêm về sinh viên
        public string? AvatarUrl { get; set; } // URL của ảnh đại diện sinh viên

        public int KhoaId { get; set; }
        public Khoa? Khoa { get; set; }

        public ICollection<MonHoc>? Subjects { get; set; } // Các môn giảng dạy
        public ICollection<LopHocPhan>? SubjectClasses { get; set; } // Các lớp học phần

        public string? UserId { get; set; } // ID của người dùng liên kết với sinh viên

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
