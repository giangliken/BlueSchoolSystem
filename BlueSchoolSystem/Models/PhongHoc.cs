namespace BlueSchoolSystem.Models
{
    public class PhongHoc
    {
        public int Id { get; set; } // Mã phòng học
        public string MaPhongHoc { get; set; } // Mã phòng học
        public string TenPhongHoc { get; set; } // Tên phòng học
        public int SoChoNgoi { get; set; } // Số chỗ ngồi trong phòng học
        public string? MoTa { get; set; } // Mô tả về phòng học
        
    }
}
