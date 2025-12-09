namespace BlueSchoolSystem.Models.ViewModel
{
    public class LichGiangDayViewModel
    {
        public string MaLopHocPhan { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public string MaPhongHoc { get; set; }

        public DateTime Ngay { get; set; }
        public string GioBatDau { get; set; }
        public string GioKetThuc { get; set; }

        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }

        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }
        public int SoLuongSinhVien { get; set; }
    }
}
