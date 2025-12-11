using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class ChiTietHocPhi
    {
        [Key]
        public int Id { get; set; }

        // 1. KHÓA NGOẠI MỚI: Liên kết đến bảng HocPhi (Bảng dư nợ tổng)
        public int HocPhiId { get; set; }

        // 2. Navigation Property cập nhật theo
        [ForeignKey("HocPhiId")]
        public virtual HocPhi HocPhi { get; set; }

        // ---------------------------------------------------------

        // Giữ nguyên liên kết với môn học/đăng ký để biết tiền này của môn nào
        public int DangKyHocPhanId { get; set; }

        [ForeignKey("DangKyHocPhanId")]
        public virtual DangKyHocPhan DangKyHocPhan { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; } // Số tiền phát sinh (Ghi nợ)

        public DateTime NgayPhatSinh { get; set; } = DateTime.Now;

    }
}