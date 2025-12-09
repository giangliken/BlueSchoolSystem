namespace BlueSchoolSystem.Models
{
    public class XinVangDay
    {
        public int Id { get; set; }

        // Giảng viên xin vắng
        public int GiangVienId { get; set; }
        public GiangVien? GiangVien { get; set; }

        // Lớp học phần
        public int LopHocPhanId { get; set; }
        public LopHocPhan? LopHocPhan { get; set; }

        // Buổi muốn nghỉ
        public int LichHocId { get; set; }
        public LichHoc? LichHoc { get; set; }

        public string LyDo { get; set; } = string.Empty;

        
        // Buổi dạy bù (do Admin duyệt)
        public DateTime? NgayDayBu { get; set; }
        public TimeSpan? GioBatDauDayBu { get; set; }
        public TimeSpan? GioKetThucDayBu { get; set; }
        public int? PhongHocId { get; set; }
        public PhongHoc? PhongHoc { get; set; }

        // Trạng thái: Pending, Approved, Rejected
        public int TrangThaiId { get; set; }
        public TrangThai? TrangThai { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
