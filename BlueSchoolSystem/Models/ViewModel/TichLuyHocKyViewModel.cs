namespace BlueSchoolSystem.Models.ViewModel
{
    public class TichLuyHocKyViewModel
    {
        public string TenHocKy { get; set; }
        public string DiemTBHocKy { get; set; }       // GPA học kỳ
        public string DiemTBTichLuy { get; set; }     // GPA tích lũy
        public int TinChiDat { get; set; }            // Tổng tín chỉ đạt
        public int TongTinChiTichLuy { get; set; }    // Tổng tín chỉ tích lũy
    }
}
