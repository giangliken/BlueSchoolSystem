namespace BlueSchoolSystem.Models.ViewModel
{
    public class LopHocPhanHocKyViewModel
    {
        public int HocKyId { get; set; }
        public string TenHocKy { get; set; }
        public DateTime NgayBatDau { get; set; }
        public List<LopHocPhanMonHocViewModel> DanhSachMon { get; set; } = new List<LopHocPhanMonHocViewModel>();
    }
}
