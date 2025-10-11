namespace BlueSchoolSystem.Models
{
    public class DiemDanh
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireAt { get; set; }

        public DateTime Ngay { get; set; } // Ngày điểm danh

        public int TrangThaiId { get; set; } // 0 - Vắng, 1 - Có mặt, 2 - Muộn
        public TrangThai? TrangThai { get; set; }

        public string? GhiChu { get; set; }


    }
}
