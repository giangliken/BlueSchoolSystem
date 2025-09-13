namespace BlueSchoolSystem.Models.ViewModel
{
    public class LichThiViewModel
    {
        public string MSSV { get; set; }
        public string TenHocKy { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public string MaLopHocPhan { get; set; }
        public string TenLopHocPhan { get; set; }
        public DateTime NgayThi { get; set; }
        public TimeSpan GioBatDauThi { get; set; }
        public TimeSpan GioKetThucThi { get; set; }
        public string MaPhongHoc { get; set; }
        public string HinhThucThi { get; set; }
        public string TinhTrangLichThi { get; set; }
        // Tính số phút tự động
        public int SoPhut
        {
            get
            {
                return (int)(GioKetThucThi - GioBatDauThi).TotalMinutes;
            }
        }
    }
}
