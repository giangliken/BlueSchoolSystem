using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class GiaoDichThanhToan
    {
        [Key]
        public int Id { get; set; }

        // Liên kết với Tài khoản sinh viên (Để biết tiền của ai)
        public int TaiKhoanSinhVienId { get; set; }
        [ForeignKey("TaiKhoanSinhVienId")]
        public virtual TaiKhoanSinhVien TaiKhoanSinhVien { get; set; }

        // Liên kết với Hóa đơn (Có thể null nếu chỉ là Nạp tiền vào ví)
        public int? HoaDonHocPhiId { get; set; }
        [ForeignKey("HoaDonHocPhiId")]
        public virtual HoaDonHocPhi? HoaDonHocPhi { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; } // Số tiền giao dịch (+ là nạp, - là thanh toán)

        public int LoaiGiaoDich { get; set; } // 1: Nạp tiền, 2: Thanh toán học phí, 3: Hoàn tiền

        public DateTime NgayThanhToan { get; set; } = DateTime.Now;
        public string HinhThuc { get; set; } // "TienMat", "VNPAY", "Momo"
        public string NguoiThucHien { get; set; } // Admin hoặc "System"
        public string? GhiChu { get; set; }
    }
}
