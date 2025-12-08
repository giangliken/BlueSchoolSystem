namespace BlueSchoolSystem.Models.ViewModel
{
    public class ApiResponseWrapper
    {
        public bool result { get; set; }
        public ChiTietLopHocApiModel data { get; set; }
    }

    public class ChiTietLopHocApiModel
    {
        public string maLop { get; set; }
        public string tenLop { get; set; }
        public SinhVienViewModel lopTruong { get; set; }
        public SinhVienViewModel lopPho { get; set; }
        public SinhVienViewModel biThu { get; set; }
        public List<SinhVienViewModel> sinhVien { get; set; }
    }


    public class SinhVienViewModel
    {
        public int Id { get; set; }
        public string MSSV { get; set; }
        public string HoVaTenDem { get; set; }
        public string Ten { get; set; }
    }

}
