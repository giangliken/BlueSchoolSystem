namespace BlueSchoolSystem.Models.ViewModel
{
    public class AttendanceSessionViewModel
    {
        public int Id { get; set; }
        public DateTime Ngay { get; set; }
        public string Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireAt { get; set; }
        public string GhiChu { get; set; }
        public int TrangThaiId { get; set; }
    }

}
