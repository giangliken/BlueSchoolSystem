using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class PhieuThu
    {
        [Key]
        public int Id { get; set; }
        public int SinhVienId { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTienDong { get; set; } // Số tiền sinh viên đóng

        public DateTime NgayDong { get; set; } = DateTime.Now;
        public string NguoiThu { get; set; } // Admin
        public string? GhiChu { get; set; }
    }
}
