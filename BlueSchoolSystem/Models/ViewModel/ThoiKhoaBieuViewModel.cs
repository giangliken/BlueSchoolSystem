namespace BlueSchoolSystem.Models.ViewModel
{
    public class ThoiKhoaBieuViewModel
    {

        public string MaLopHocPhan { get; set; } = string.Empty;
        public string TenLopHocPhan { get; set; } = string.Empty;
        public string MaMonHoc { get; set; } = string.Empty;
        public string TenMonHoc { get; set; } = string.Empty;
        public string TenGiangVien { get; set; } = string.Empty;
        public string MaPhongHoc { get; set; } = string.Empty;

        public DateTime NgayHoc { get; set; }
        public string? GioBatDau { get; set; }
        public string? GioKetThuc { get; set; }

        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }

        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
    }
}
