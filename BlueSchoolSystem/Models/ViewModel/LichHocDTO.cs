using System.ComponentModel.DataAnnotations;

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
        [Range(40, 120, ErrorMessage = "Sĩ số phải từ 40 đến 120.")]
        public int SiSoToiDa { get; set; }
        public int PhongHocId { get; set; }
        // Ngày bắt đầu/kết thúc LHP (có thể khác với HK)
        public DateTime NgayBatDauLHP { get; set; }
        public DateTime NgayKetThucLHP { get; set; }

        //public List<LichHocDTO> LichHocs { get; set; } = new List<LichHocDTO>();

        public bool ShouldAutoCreateSchedule { get; set; } // Cờ báo có muốn tự động tạo không
        public TimeSpan? AutoGioBatDau { get; set; }
        public TimeSpan? AutoGioKetThuc { get; set; }
        public int? AutoPhongHocId { get; set; }
        // 2=Thứ Hai, ..., 8=Chủ Nhật
        public List<int>? AutoCacNgayTrongTuan { get; set; }
    }

    // DTO cho việc Hủy/Mở lại lớp
    public class UpdateLopHocPhanStatusDTO
    {
        public int LopHocPhanId { get; set; }
        public int TrangThaiId { get; set; } // ID của trạng thái mới (Đang mở / Bị hủy / Khóa đăng ký) [cite: 37]
        public string? LyDo { get; set; }
    }
    public class AutoAddScheduleDTO
    {
        public int LopHocPhanId { get; set; }

        // Dùng int cho Tiết để chọn trên View
        [Required(ErrorMessage = "Vui lòng chọn Tiết Bắt Đầu.")]
        [Range(1, 15, ErrorMessage = "Tiết học phải từ 1 đến 15.")]
        public int TietBatDau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Tiết Kết Thúc.")]
        [Range(1, 15, ErrorMessage = "Tiết học phải từ 1 đến 15.")]
        public int TietKetThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phòng Học.")]
        public int PhongHocId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một ngày trong tuần.")]
        public List<int> CacNgayTrongTuan { get; set; } = new List<int>();

        // BỎ constructor cũ và thuộc tính GioBatDau/GioKetThuc TimeSpam
        public AutoAddScheduleDTO() { }
    }
    public class AddScheduleDTO
    {
        // ID của LHP đang thêm lịch
        public int LopHocPhanId { get; set; }

        // NGÀY CỤ THỂ cần thêm lịch
        [Required(ErrorMessage = "Vui lòng chọn Ngày học.")]
        public DateTime Ngay { get; set; }

        // Dùng int cho Tiết để chọn trên View
        [Required(ErrorMessage = "Vui lòng chọn Tiết Bắt Đầu.")]
        [Range(1, 15, ErrorMessage = "Tiết học phải từ 1 đến 15.")]
        public int TietBatDau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Tiết Kết Thúc.")]
        [Range(1, 15, ErrorMessage = "Tiết học phải từ 1 đến 15.")]
        public int TietKetThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phòng Học.")]
        public int PhongHocId { get; set; }
    }
}
