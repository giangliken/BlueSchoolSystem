// Trong file BlueSchoolSystem.Models.ViewModel/QuanLyDangKyLHPRequestDTO.cs

using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace BlueSchoolSystem.Models.ViewModel
{

    public class QuanLyDangKyLHPRequestDTO
    {
        [Required(ErrorMessage = "Phải chọn ít nhất một Lớp Học Phần.")]
        public List<int> LopHocPhanIds { get; set; } = new List<int>();

        [Required(ErrorMessage = "Ngày bắt đầu đăng ký là bắt buộc.")]
        [DataType(DataType.DateTime)]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc đăng ký là bắt buộc.")]
        [DataType(DataType.DateTime)]
        public DateTime NgayKetThuc { get; set; }

        [Required(ErrorMessage = "Phải chỉ định ID Học kỳ.")]
        public int HocKyId { get; set; }

        [Required(ErrorMessage = "Phải đặt Tên Đợt Đăng ký.")]
        [StringLength(100)]
        public string TenDotDangKy { get; set; }
    }
}