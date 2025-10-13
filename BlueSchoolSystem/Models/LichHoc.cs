namespace BlueSchoolSystem.Models
{
    public class LichHoc
    {
        public int Id { get; set; }
        public int LopHocPhanId { get; set; }
        public LopHocPhan? LopHocPhan { get; set; }
        public DateTime Ngay { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public int? PhongHocId { get; set; }
        public PhongHoc? PhongHoc { get; set; }
    }
}
