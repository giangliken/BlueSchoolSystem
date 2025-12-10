using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    [Table("TaiKhoanSinhViens")]
    public class TaiKhoanSinhVien
    {
        [Key]
        public int Id { get; set; }

        // Liên kết 1-1 với SinhVien
        [ForeignKey("SinhVien")]
        public int SinhVienId { get; set; }
        public virtual SinhVien SinhVien { get; set; }

        // Số dư hiện tại
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoDu { get; set; } = 0;

        // Trạng thái tài khoản (VD: True = Đang hoạt động, False = Bị khóa nợ học phí quá lâu)
        public int TrangThaiId { get; set; }
        public TrangThai? TrangThai { get; set; }

        // Ngày cập nhật số dư cuối cùng
        public DateTime NgayCapNhatCuoi { get; set; } = DateTime.Now;

        // Concurrency Token: Chống lỗi khi 2 admin cùng nạp tiền 1 lúc
        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Liên kết với lịch sử giao dịch (để biết tiền vào ra thế nào)
        public virtual ICollection<GiaoDichThanhToan> LichSuGiaoDichs { get; set; }
    }
}
