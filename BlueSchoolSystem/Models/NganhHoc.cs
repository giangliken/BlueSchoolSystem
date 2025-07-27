using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models
{
    public class NganhHoc
    {

        public int Id { get; set; } // Primary Key, có thể là Id tự động tăng
        public string MaNganh { get; set; } 
        public string TenNganh { get; set; }

        // FK về Khoa (nếu muốn mở rộng)
        public int KhoaId { get; set; }
        public Khoa? Khoa { get; set; }

    }
}
