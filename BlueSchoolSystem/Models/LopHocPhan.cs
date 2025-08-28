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
        public PhongHoc? PhongHoc { get; set; } // Tham chiếu đến phòng học
        public int? Thu { get; set; } // Thứ trong tuần (ví dụ: Thứ 2, Thứ 3, ...)
        public TimeSpan? GioBatDau { get; set; } // Giờ bắt đầu buổi học
        public TimeSpan? GioKetThuc { get; set; } // Giờ kết thúc buổi học
        public DateTime NgayBatDau { get; set; } // Ngày bắt đầu lớp học phần
        public DateTime NgayKetThuc { get; set; } // Ngày kết thúc lớp học phần
        public int SiSo { get; set; } // Sĩ số tối đa của lớp học phần
        public string TrangThai { get; set; } // Trạng thái lớp học phần (ví dụ: Đang diễn ra, Đã kết thúc, Hủy)
    }
}
