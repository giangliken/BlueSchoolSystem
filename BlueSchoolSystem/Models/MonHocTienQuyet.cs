using System.ComponentModel.DataAnnotations.Schema;

namespace BlueSchoolSystem.Models
{
    public class MonHocTienQuyet
    {
        public int Id { get; set; }

        // 🔹 Môn học chính (môn mà SV muốn học)
        public int MonHocId { get; set; }
        public MonHoc MonHoc { get; set; }

        // 🔹 Môn tiên quyết (môn phải học trước)
        public int MonHocTienQuyetId { get; set; }
        [ForeignKey(nameof(MonHocTienQuyetId))]
        public MonHoc MonHocBatBuoc { get; set; }
    }
}
