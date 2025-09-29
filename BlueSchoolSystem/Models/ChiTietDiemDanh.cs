namespace BlueSchoolSystem.Models
{
    public class ChiTietDiemDanh
    {
        public int Id { get; set; }
        public int DiemDanhId { get; set; } // ID của điểm danh
        public DiemDanh? DiemDanh { get; set; } // Tham chiếu đến điểm danh
        public int SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên
        public DateTime ThoiGian { get; set; } // Thời gian điểm danh
        public int TrangThaiId { get; set; } // Trạng thái điểm danh (ví dụ: 0 - Vắng, 1 - Có mặt, 2 - Muộn)
        public TrangThai? TrangThai { get; set; } // Liên kết với bảng trạng thái
        public string? GhiChu { get; set; } // Ghi chú thêm về chi tiết điểm danh

    }
}
