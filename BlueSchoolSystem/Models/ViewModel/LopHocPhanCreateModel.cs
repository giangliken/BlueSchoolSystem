using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models.ViewModel
{
    // Mô hình cho dữ liệu gửi lên để tạo một LHP mới
    public class LopHocPhanCreateModel
    {
        // Thuộc tính LHP
        [Required]
        public string MaLopHocPhan { get; set; }
        [Required]
        public string TenLopHocPhan { get; set; }
        public int MonHocId { get; set; }
        public int? GiangVienId { get; set; }
        public int HocKyId { get; set; }
        public int SiSoToiDa { get; set; } // Mapping đến LopHocPhan.SiSo


    }


}