using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class HoaDonHocPhi
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("SinhVien")]
        public int SinhVienId { get; set; }

        // Liên kết đến bảng SinhViens hiện có của bạn
        public virtual SinhVien SinhVien { get; set; }

        public string NamHoc { get; set; } // VD: "2024-2025"
        public int HocKy { get; set; } // VD: 1

        // Tổng số tiền phải đóng
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTien { get; set; }

        // Số tiền đã đóng (cập nhật mỗi khi thanh toán)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DaDong { get; set; }

        // Số tiền còn nợ (TongTien - DaDong)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ConLai { get; set; }

        // Hạn nộp
        public DateTime? HanNop { get; set; }

        // Trạng thái (FK đến bảng TrangThais của bạn)
        // Ví dụ: 1-Chưa đóng, 2-Đóng một phần, 3-Đã hoàn thành
        public int TrangThaiId { get; set; }
        public TrangThai? TrangThai { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public string? GhiChu { get; set; }

        // Danh sách chi tiết (nếu muốn liệt kê từng môn trong hóa đơn)
        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    }
}
