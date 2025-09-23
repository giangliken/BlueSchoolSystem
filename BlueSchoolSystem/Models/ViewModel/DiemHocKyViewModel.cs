namespace BlueSchoolSystem.Models.ViewModel
{
    public class DiemHocKyViewModel
    {
        public int HocKyId { get; set; }
        public string TenHocKy { get; set; }
        public DateTime NgayBatDau { get; set; }
        public List<DiemMonHocViewModel> Diems { get; set; } = new List<DiemMonHocViewModel>();
    }
}
