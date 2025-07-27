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
    }
}
