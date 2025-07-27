namespace BlueSchoolSystem.Models
{
    public class LopHocPhan
    {
        public int Id { get; set; } // Mã lớp học phần
        public string MaLopHocPhan { get; set; } // Mã lớp học phần
        public string TenLopHocPhan { get; set; } // Tên lớp học phần
        public int SoTinChi { get; set; } // Số tín chỉ của lớp học phần
        public string? MoTa { get; set; } // Mô tả về lớp học phần
        public int MonHocId { get; set; } // Mã môn học liên kết
        public MonHoc? MonHoc { get; set; } // Môn học liên kết
        public int GiangVienId { get; set; } // Mã giảng viên liên kết
        public GiangVien? GiangVien { get; set; } // Giảng viên liên kết
        public DateTime CreatedAt { get; set; } // Ngày tạo bản ghi
        public DateTime UpdatedAt { get; set; } // Ngày cập nhật bản ghi
        public LopHocPhan()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
