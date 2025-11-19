namespace BlueSchoolSystem.Models.ViewModel
{
    public class DotDangKyDTO
    {
        public int HocKyId { get; set; }
        public string TenDot { get; set; } = default!;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        // Lưu trữ các tiêu chí áp dụng: Ví dụ: KHOA-K2023; LOAIHINH-VB2
        public List<DoiTuongApDungDTO> DoiTuongApDungs { get; set; } = new List<DoiTuongApDungDTO>();
        public string LoaiThaoTac { get; set; } = "DANGKY";
    }
    public class DoiTuongApDungDTO
    {
        public string Loai { get; set; } // KHOA, NGANH, LOAIHINH 
        public string GiaTri { get; set; } // Mã Khóa (K2023), Mã Ngành (CT), Loại hình (VB2) [cite: 13, 17]
    }
}
