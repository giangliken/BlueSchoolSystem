namespace BlueSchoolSystem.Models
{
    public class LopHocPhan
    {
        public int Id { get; set; } // Mã lớp học phần
        public string MaLopHocPhan { get; set; } // Mã lớp học phần
        public string TenLopHocPhan { get; set; } // Tên lớp học phần
        public string? MoTa { get; set; } // Mô tả về lớp học phần
        public int MonHocId { get; set; } // Mã môn học liên kết
        public MonHoc? MonHoc { get; set; } // Môn học liên kết
        public int? GiangVienId { get; set; } // Mã giảng viên liên kết
        public GiangVien? GiangVien { get; set; } // Giảng viên liên kết
        //Thông tin phòng học
        public int? PhongHocId { get; set; } // ID của phòng học
        public DateTime NgayBatDau { get; set; } // Ngày bắt đầu lớp học phần
        public DateTime NgayKetThuc { get; set; } // Ngày kết thúc lớp học phần
        public int SiSo { get; set; } // Sĩ số tối đa của lớp học phần
        public int TrangThaiId { get; set; } // Trạng thái lớp học phần (ví dụ: Đang diễn ra, Đã kết thúc, Hủy)
        public TrangThai? TrangThai { get; set; } // Trạng thái lớp học phần

        public ICollection<BangDiem> BangDiems { get; set; }
        public ICollection<DiemDanh> DiemDanhs { get; set; } = new List<DiemDanh>();

        //Học kì
        public int HocKyId { get; set; } // Mã học kỳ liên kết
        public HocKy? HocKy { get; set; } // Học kỳ liên kết
    }
}
