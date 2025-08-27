namespace BlueSchoolSystem.Models
{
    public class DiemDanh
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần
        public int SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên
        public DateTime Ngay { get; set; } // Ngày điểm danh
        public int TrangThai { get; set; } // Trạng thái điểm danh (ví dụ: 0 - Vắng, 1 - Có mặt, 2 - Muộn)
        public string? GhiChu { get; set; } // Ghi chú thêm về điểm danh

    }
}
