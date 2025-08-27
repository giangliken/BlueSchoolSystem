namespace BlueSchoolSystem.Models
{
    public class ChiTietLopHocPhan
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần

        public int? SinhVienId { get; set; } // ID của sinh viên
        public SinhVien? SinhVien { get; set; } // Tham chiếu đến sinh viên

        //Thông tin phòng học
        public int? PhongHocId { get; set; } // ID của phòng học
        public PhongHoc? PhongHoc { get; set; } // Tham chiếu đến phòng học
        public int? Thu { get; set; } // Thứ trong tuần (ví dụ: Thứ 2, Thứ 3, ...)
        
        public TimeSpan? GioBatDau { get; set; } // Giờ bắt đầu buổi học
        public TimeSpan? GioKetThuc { get; set; } // Giờ kết thúc buổi học

    }
}
