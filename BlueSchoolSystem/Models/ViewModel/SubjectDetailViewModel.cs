namespace BlueSchoolSystem.Models.ViewModel
{
    public class SubjectDetailViewModel
    {
        public int Id { get; set; }
        public string MaMonHoc { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public string? MoTa { get; set; }

        public List<NganhItem> Nganhs { get; set; } = new();
        public List<GiangVienItem> GiangViens { get; set; } = new();
    }

    public class NganhItem
    {
        public string MaNganh { get; set; }
        public string TenNganh { get; set; }
    }

    public class GiangVienItem
    {
        public int Id { get; set; }
        public string MaGiangVien { get; set; }
        public string HoTen { get; set; }
    }
}
