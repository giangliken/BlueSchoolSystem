namespace BlueSchoolSystem.Models
{
    public class LichThi
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; } // Mã lớp học phần
        public LopHocPhan LopHocPhan { get; set; } // Tham chiếu đến lớp học phần
        public DateTime NgayThi { get; set; } // Ngày thi
        public TimeSpan GioBatDau { get; set; } // Giờ thi
        public TimeSpan GioKetThuc { get; set; } // Giờ kết thúc thi
        public string HinhThucThi { get; set; } // Hình thức thi
        public int PhongHocId { get; set; } // Mã phòng học
        public PhongHoc PhongHoc { get; set; } // Tham chiếu đến phòng học
        public int? TrangThaiId { get; set; } // Cho phép null
        public TrangThai? TrangThai { get; set; } // Quan hệ nhiều-đến-1 có thể null

    }
}
