namespace BlueSchoolSystem.Models
{
    public class SuKien
    {
        public int Id { get; set; }
        public string TenSuKien { get; set; }
        public int KhoaId { get; set; }
        public Khoa? Khoa { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string? DiaDiem { get; set; }
        public string? MoTa { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
