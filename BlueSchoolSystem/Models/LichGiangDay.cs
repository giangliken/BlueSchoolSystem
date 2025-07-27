namespace BlueSchoolSystem.Models
{
    public class LichGiangDay
    {
        public int Id { get; set; } // Mã lịch giảng dạy
        public int GiangVienId { get; set; } // Mã giảng viên liên kết
        public GiangVien? GiangVien { get; set; } // Giảng viên liên kết
        public int LopHocPhanId { get; set; } // Mã lớp học phần liên kết
        public LopHocPhan? LopHocPhan { get; set; } // Lớp học phần liên kết
        public DateTime NgayGiangDay { get; set; } // Ngày giảng dạy
        public TimeSpan ThoiGianBatDau { get; set; } // Thời gian bắt đầu giảng dạy
        public TimeSpan ThoiGianKetThuc { get; set; } // Thời gian kết thúc giảng dạy
        public string PhongHoc { get; set; } // Phòng học
        public DateTime CreatedAt { get; set; } // Ngày tạo bản ghi
        public DateTime UpdatedAt { get; set; } // Ngày cập nhật bản ghi
        public LichGiangDay()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
