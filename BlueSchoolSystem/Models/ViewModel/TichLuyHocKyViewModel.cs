namespace BlueSchoolSystem.Models.ViewModel
{
    public class TichLuyHocKyViewModel
    {
        public string TenHocKy { get; set; }
        public double DiemTBHocKy { get; set; }       // GPA học kỳ
        public double DiemTBTichLuy { get; set; }     // GPA tích lũy
        public int TinChiDat { get; set; }            // Tổng tín chỉ đạt
        public int TongTinChiTichLuy { get; set; }    // Tổng tín chỉ tích lũy
    }
}
