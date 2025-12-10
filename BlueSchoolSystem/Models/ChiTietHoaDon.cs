using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int Id { get; set; }

        public int HoaDonHocPhiId { get; set; }
        [ForeignKey("HoaDonHocPhiId")]
        public virtual HoaDonHocPhi HoaDonHocPhi { get; set; }

        // 1. Khai báo cột chứa dữ liệu ID (Giữ nguyên, bỏ Attribute cũ đi)
        public int DangKyHocPhanId { get; set; }

        [ForeignKey("DangKyHocPhanId")]
        public virtual DangKyHocPhan DangKyHocPhan { get; set; }

        // Lưu lại giá tại thời điểm tạo hóa đơn
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }

    }
}