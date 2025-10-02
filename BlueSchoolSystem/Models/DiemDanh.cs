namespace BlueSchoolSystem.Models
{
    public class DiemDanh
    {
        public int Id { get; set; }

        public int ChiTietLopHocPhanId { get; set; } // Liên kết đến SV trong lớp học phần
        public ChiTietLopHocPhan? ChiTietLopHocPhan { get; set; }

        public DateTime Ngay { get; set; } // Ngày điểm danh

        public int TrangThaiId { get; set; } // 0 - Vắng, 1 - Có mặt, 2 - Muộn
        public TrangThai? TrangThai { get; set; }

        public string? GhiChu { get; set; }


    }
}
