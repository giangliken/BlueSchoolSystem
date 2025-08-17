namespace BlueSchoolSystem.Models.ViewModel
{
    public class UpdateStudentRequest
    {
        public string? MSSV { get; set; }
        public string? HoVaTenDem { get; set; }
        public string? Ten { get; set; }
        public string? CCCD { get; set; }
        public DateTime? NgaySinh { get; set; }
        public bool? GioiTinh { get; set; }
        public string? DiaChi { get; set; }
        public int? LopId { get; set; }
        public DateTime? NgayNhapHoc { get; set; }
        public DateTime? NgayTotNghiep { get; set; }
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
