using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; 

namespace BlueSchoolSystem.Models.ViewModel
{

    public class AutoEnrollmentRequestDTO
    {
        [Required]
        public int HocKyId { get; set; }
        [Required]
        public int NganhId { get; set; }
        [Required]
        [Range(2000, 3000)]
        public int KhoaNhapHoc { get; set; }
        [Required]
        [Range(1, 18)]
        public int ThuTuHocKy { get; set; }
    }


    public class AutoEnrollmentRequestWithScheduleDTO : AutoEnrollmentRequestDTO
    {
        public bool ShouldAutoCreateSchedule { get; set; } = true;

        [Required(ErrorMessage = "Phải chọn Phòng học mặc định.")]
        public int DefaultPhongHocId { get; set; }

        [Required(ErrorMessage = "Phải chọn Tiết bắt đầu.")]
        [Range(1, 15)]
        public int TietBatDau { get; set; }

        [Required(ErrorMessage = "Phải chọn Tiết kết thúc.")]
        [Range(1, 15)]
        public int TietKetThuc { get; set; }

        [Required(ErrorMessage = "Phải chọn ít nhất một Ngày trong tuần.")]
        public List<int> CacNgayTrongTuan { get; set; } = new List<int>();

    }

    // Kế thừa từ class WithSchedule để lấy hết các trường lịch học
    public class AutoEnrollmentApiPayload : AutoEnrollmentRequestWithScheduleDTO
    {
        // Chỉ thêm trường này là Required cho API
        [Required]
        public List<string> MandatorySubjectCodes { get; set; } = new List<string>();
    }


}