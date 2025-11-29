namespace BlueSchoolSystem.Models
{
    public class HocKy
    {
        public int Id { get; set; } // Mã học kỳ
        public string TenHocKy { get; set; } // Tên học kỳ 
        public DateTime NgayBatDau { get; set; } // Ngày bắt đầu học
        public DateTime NgayKetThuc { get; set; } // Ngày kết thúc học

        // cho phép đăng ký học phần
        public int TrangThaiId { get; set; } 
        public TrangThai? TrangThai { get; set; }
    }
}
