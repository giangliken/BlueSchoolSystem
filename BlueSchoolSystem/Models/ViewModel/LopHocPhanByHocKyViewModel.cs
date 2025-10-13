using BlueSchoolSystem.Models.ViewModel;

public class LopHocPhanByHocKyViewModel
{
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; }
    public DateTime NgayBatDau { get; set; }
    public List<LopHocPhanViewModel> LopHocPhans { get; set; }
}
public class LopHocPhanFilterViewModel
{
    public List<LopHocPhanByHocKyViewModel> DanhSachHocKy { get; set; }
    public int SelectedHocKyId { get; set; }
    public List<LopHocPhanViewModel> LopHocPhans { get; set; }
}
