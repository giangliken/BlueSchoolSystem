using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class DinhMucHocPhi
    {
        [Key]
        public int Id { get; set; }

        // Ví dụ: Ngành CNTT giá khác ngành Kinh tế
        public int? NganhId { get; set; }
        [ForeignKey("NganhId")]
        public virtual NganhHoc? NganhHoc { get; set; }

        // Năm học áp dụng (ví dụ: 2024-2025)
        public string NamHoc { get; set; }

        // Giá tiền cho 1 tín chỉ (VNĐ)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal GiaTienMotTinChi { get; set; }


    }
}
