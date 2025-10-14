namespace BlueSchoolSystem.Models.ViewModel
{
    public class LopHocPhanResponseViewModel
    {
        public bool Result { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public int SoLuongHocKy { get; set; }
        public List<LopHocPhanHocKyViewModel> Data { get; set; }
    }
}
