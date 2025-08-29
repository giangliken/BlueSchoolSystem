using System.ComponentModel.DataAnnotations;

namespace BlueSchoolSystem.Models.ViewModel
{
    public class LopHocViewModel
    {
        public int Id { get; set; }
        public string MaLop { get; set; }
        public string TenLop { get; set; }

        public string? MoTa { get; set; }

        public int? NganhId { get; set; }
        public string Nganh { get; set; }

        public int? KhoaId { get; set; }
        public string Khoa { get; set; }

        public int SiSo { get; set; }

        public ICollection<SinhVien> Students { get; set; } = new List<SinhVien>();
    }
}
