namespace BlueSchoolSystem.Models.ViewModel
{
    public class UpdateGiangVienRequest
    {
        public string? HoVaTenDem { get; set; }
        public string? Ten { get; set; }
        public bool? GioiTinh { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? CCCD { get; set; }
        public string? DiaChi { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? TrangThaiId { get; set; }
        public string? GhiChu { get; set; }
    }
}
