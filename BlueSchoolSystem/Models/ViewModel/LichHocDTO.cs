namespace BlueSchoolSystem.Models.ViewModel
{
    public class LichHocDTO
    {
        public int LopHocPhanId { get; set; }
        public DateTime Ngay { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public int PhongHocId { get; set; }
    }
    // DTO để tạo mới Lớp Học Phần
    public class LopHocPhanCreateDTO
    {
        public int HocKyId { get; set; }
        public int MonHocId { get; set; }
        public int GiangVienId { get; set; }
        public string MaLopHocPhan { get; set; } // Mã nhóm (vd: CTDLGT_01) [cite: 33]
        public string TenLopHocPhan { get; set; }
        public int SiSoToiDa { get; set; } // Sĩ số tối đa [cite: 36]

        // Ngày bắt đầu/kết thúc LHP (có thể khác với HK)
        public DateTime NgayBatDauLHP { get; set; }
        public DateTime NgayKetThucLHP { get; set; }

        public List<LichHocDTO> LichHocs { get; set; } = new List<LichHocDTO>();
    }

    // DTO cho việc Hủy/Mở lại lớp
    public class UpdateLopHocPhanStatusDTO
    {
        public int LopHocPhanId { get; set; }
        public int TrangThaiId { get; set; } // ID của trạng thái mới (Đang mở / Bị hủy / Khóa đăng ký) [cite: 37]
        public string? LyDo { get; set; }
    }
}
