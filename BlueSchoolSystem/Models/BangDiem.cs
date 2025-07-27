namespace BlueSchoolSystem.Models
{
    public class BangDiem
    {
        public int Id { get; set; }
        public int SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên
        public int MonHocId { get; set; } // ID của môn học
        public MonHoc? MonHoc { get; set; } // Tham chiếu đến môn học
        public float DiemChuyenCan { get; set; } // Điểm chuyên cần
        public float? DiemCuoiKy { get; set; } // Điểm cuối kỳ
        
    }
}
