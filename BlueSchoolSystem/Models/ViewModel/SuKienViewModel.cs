namespace BlueSchoolSystem.Models.ViewModel
{
    public class SuKienViewModel
    {
        public int Id { get; set; }
        public string TenSuKien { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string? DiaDiem { get; set; }
        public string? MoTa { get; set; }

    }
}
