namespace BlueSchoolSystem.Models.ViewModel
{
    public class DangKyHocPhanVM
    {
        public int Id { get; set; }
        public string MaLopHocPhan { get; set; }
        public string MaMonHoc { get; set; } // Mới
        public string TenMonHoc { get; set; }
        public int SoTinChi { get; set; }
        public string GiangVien { get; set; }

        public string LichHoc { get; set; } // Chuỗi đã format để hiển thị

        public DateTime NgayBatDau { get; set; } // Mới
        public DateTime NgayKetThuc { get; set; } // Mới

        public int SiSo { get; set; }
        public int DaDangKy { get; set; }
        public int ChoTrong => SiSo - DaDangKy; // Mới: Tự động tính

        public bool IsRegistered { get; set; }
        public string TrangThaiLop { get; set; }

        public List<LichHocChiTietVM> LichHocChiTiet { get; set; } = new List<LichHocChiTietVM>();
    }

    public class LichHocChiTietVM
    {
        public string Thu { get; set; }       // T2, T3...
        public int TietBatDau { get; set; }
        public int SoTiet { get; set; }
        public string Phong { get; set; }
    }
}
