namespace BlueSchoolSystem.Models.ViewModel
{
    public class LopHocPhanViewModel
    {
        public int Id { get; set; }
        public string MaLopHocPhan { get; set; } = "";
        public string TenLopHocPhan { get; set; } = "";
        public string MaMonHoc { get; set; } = "";
        public string TenMonHoc { get; set; } = "";
        public int Thu { get; set; } 
        public string GioBatDau { get; set; } = ""; 
        public string GioKetThuc { get; set; } = "";
        public DateTime NgayBatDauLop { get; set; }
        public DateTime NgayKetThucLop { get; set; }

        public int SiSo { get; set; }
        public int SiSoThucTe { get; set; }
        public List<SinhVienViewModel> DanhSachSinhVien { get; set; } = new List<SinhVienViewModel>();

        public TrangThaiViewModel? TrangThai { get; set; }
    }

    public class TrangThaiViewModel
    {
        public int Id { get; set; }
        public string TenTrangThai { get; set; } = "";
        public string MoTa { get; set; } = "";
        public string LoaiTrangThai { get; set; } = "";
    }

}
