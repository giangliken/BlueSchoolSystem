namespace BlueSchoolSystem.Models.ViewModel
{
    public class XinVangDayViewModel
    {
        public int Id { get; set; }
        public string MaGiangVien { get; set; }

        public string TenGiangVien { get; set; }
        public string LyDo { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TrangThai { get; set; }
        public string MaLopHocPhan { get; set; }
        public string TenLopHocPhan { get; set; }

        public string NgayXinVang { get; set; }
        public string CaXinVang { get; set; }
        public DateTime? NgayDayBu { get; set; }
        public TimeSpan? GioBatDauDayBu { get; set; }
        public TimeSpan? GioKetThucDayBu { get; set; }

        public string Phong { get; set; }
    }
}
