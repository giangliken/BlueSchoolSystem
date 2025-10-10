namespace BlueSchoolSystem.Models
{
    public class DangKyHocPhan
    {
        public int Id { get; set; }
        public int SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên
        public int HocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần

        public DateTime NgayDangKy { get; set; } // Ngày đăng ký học phần

        public bool LaHocVuot { get; set; } // True = học vượt, False = học đúng CTĐT
        public string? GhiChu { get; set; }

        public int TrangThaiId { get; set; } // Ví dụ: Đã duyệt, Chờ duyệt, Hủy
        public TrangThai? TrangThai { get; set; }
    }
}
