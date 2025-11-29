namespace BlueSchoolSystem.Models
{
    public class DotDangKy
    {
        public int Id { get; set; }
        public int HocKyId { get; set; }
        public HocKy? HocKy { get; set; }
        public string TenDot { get; set; } // Ví dụ: Đợt 1, Đợt điều chỉnh [cite: 13, 20]
        public DateTime NgayBatDau { get; set; } // [cite: 13]
        public DateTime NgayKetThuc { get; set; } // [cite: 13]
        public string LoaiDoiTuong { get; set; } // Ví dụ: KHOA, NGANH, LOAIHINH 
        public string GiaTriDoiTuong { get; set; } // Ví dụ: K2023, CT, VB2 [cite: 13, 17]
        public string LoaiThaoTac { get; set; } // Ví dụ: DANGKY, HUY, RUT [cite: 20, 21, 22]
        public bool IsActive { get; set; } = true;
    }
}
