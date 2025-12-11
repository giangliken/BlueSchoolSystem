using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class HocPhi
    {
        [Key]
        public int Id { get; set; }

        public int SinhVienId { get; set; }
        [ForeignKey("SinhVienId")]
        public virtual SinhVien SinhVien { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DuNoConLai { get; set; } = 0;

        public DateTime NgayCapNhatCuoi { get; set; } = DateTime.Now;

    }
}
