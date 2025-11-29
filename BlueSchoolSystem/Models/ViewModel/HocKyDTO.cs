namespace BlueSchoolSystem.Models.ViewModel
{
    public class HocKyDTO
    {
        public int Id { get; set; }
        public string TenHocKy { get; set; } = default!;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        // Ngày công bố TKB (sẽ được xử lý qua thông báo, không lưu trong DB)
        public DateTime NgayCongBoTKB { get; set; }
        public int TrangThaiId { get; set; }
    }
    public class UpdateTrangThaiDTO
    {
        public int HocKyId { get; set; }
        public int TrangThaiId { get; set; } // ID của trạng thái mới (MoDangKy, KhoaDangKy, v.v.)
    }
}
