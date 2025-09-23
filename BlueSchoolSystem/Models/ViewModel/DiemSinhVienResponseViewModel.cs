namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemSinhVienResponseViewModel
    {
        public bool Result { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public int SoLuong { get; set; }

        // Danh sách điểm nhóm theo học kỳ
        public List<DiemHocKyViewModel> Data { get; set; } = new List<DiemHocKyViewModel>();

        // Thống kê tích lũy toàn bộ
        public int TongTinChiTichLuy { get; set; }
        public int TongTinChiDat { get; set; }
        public string DiemTBTichLuy { get; set; }

        // Danh sách tích lũy theo từng học kỳ
        public List<TichLuyHocKyViewModel> TichLuyList { get; set; } = new List<TichLuyHocKyViewModel>();
    }
}
