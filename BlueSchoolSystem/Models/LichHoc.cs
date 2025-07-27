namespace BlueSchoolSystem.Models
{
    public class LichHoc
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; } // ID của lớp học phần
        public LopHocPhan? LopHocPhan { get; set; } // Tham chiếu đến lớp học phần
        public int PhongHocId { get; set; } // ID của phòng học
        public PhongHoc? PhongHoc { get; set; } // Tham chiếu đến phòng học
        public int Thu { get; set; } // Thứ trong tuần (1-7, tương ứng với Thứ Hai đến Chủ Nhật)

        public int TietBatDau { get; set; } // Tiết bắt đầu (1-15, tương ứng với các tiết học trong ngày)
        public int SoTiet { get; set; } // Số tiết học (1-15, tương ứng với các tiết học trong ngày)

    }
}
