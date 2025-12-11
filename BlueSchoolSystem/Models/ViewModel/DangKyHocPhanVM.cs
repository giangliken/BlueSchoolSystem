namespace BlueSchoolSystem.Models.ViewModel
{
    public class DangKyHocPhanVM
    {
        public int Id { get; set; } // ID Lớp học phần
        public string MaLopHocPhan { get; set; }
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public string GiangVien { get; set; }
        public string LichHoc { get; set; } // Chuỗi hiển thị lịch (VD: "Thứ 2 (Tiết 1-3)")
        public int SiSo { get; set; }
        public int DaDangKy { get; set; } // Số lượng đã đăng ký hiện tại
        public bool IsRegistered { get; set; } // Sinh viên hiện tại đã đăng ký lớp này chưa?
        public string TrangThaiLop { get; set; }
    }
}
