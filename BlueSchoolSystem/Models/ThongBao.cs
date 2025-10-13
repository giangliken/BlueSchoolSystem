using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class ThongBao
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        [Required]
        public DateTime Time { get; set; }

        // UserId của Identity (GUID string)
        [Required]
        public string ReceiverUserId { get; set; }

        // (OPTIONAL) UserId của người gửi (cũng là Identity)
        public string? SenderUserId { get; set; }

        // Loại thông báo
        [StringLength(30)]
        public string? Type { get; set; }

        // Tham chiếu tới User (optional navigation property)
        [ForeignKey("ReceiverUserId")]
        public virtual ApplicationUser ReceiverUser { get; set; }
    }
}
