namespace BlueSchoolSystem.Models
{
    public class BangDiem
    {
        public int Id { get; set; }
        public int SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên
       
        public int LopHocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần

        public float? DiemChuyenCan { get; set; } // Điểm chuyên cần
        public float? DiemCuoiKy { get; set; } // Điểm cuối kỳ
        
    }
}
