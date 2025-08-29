namespace BlueSchoolSystem.Models
{
    public class ChiTietLopHocPhan
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần

        public int? SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên

    }
}
