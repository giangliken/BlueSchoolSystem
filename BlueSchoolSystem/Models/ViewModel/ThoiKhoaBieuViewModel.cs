namespace BlueSchoolSystem.Models.ViewModel
{
    public class ThoiKhoaBieuViewModel
    {
        public string MSSV { get; set; }
        public string MaLopHocPhan { get; set; }
        public string TenLopHocPhan { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public string TenGiangVien { get; set; }
        public string MaPhongHoc { get; set; }
        public int Thu { get; set; }
        public string? GioBatDau { get; set; }
        public string? GioKetThuc { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
    }
}
