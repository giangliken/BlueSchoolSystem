namespace BlueSchoolSystem.Models.ViewModel
{
    // ViewModel cho danh sách lớp phụ trách (Items trong danh sách)
    public class LopPhuTrachViewModel
    {
        public int Id { get; set; } // Id của bảng ChiTietLopHoc
        public int LopHocId { get; set; }
        public string MaLop { get; set; }
        public string TenLop { get; set; }
        public string VaiTro { get; set; } // GVCN hoặc Cố vấn
    }

    // ViewModel cho chi tiết 1 lớp (bao gồm danh sách sinh viên)
    public class LopPhuTrachDetailViewModel
    {
        public int LopHocId { get; set; }
        public string MaLop { get; set; }
        public string TenLop { get; set; }
        public List<SinhVienViewModel> DanhSachSinhVien { get; set; } = new List<SinhVienViewModel>();
    }
}
