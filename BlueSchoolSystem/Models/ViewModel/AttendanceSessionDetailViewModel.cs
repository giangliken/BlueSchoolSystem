namespace BlueSchoolSystem.Models.ViewModel
{
    public class AttendanceSessionDetailViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public DateTime Ngay { get; set; }

        public DateTime ExpireAt { get; set; }
        public List<StudentDetailViewModel> SinhViens { get; set; } 
        public string QrCodeBase64 { get; set; }
        public string MaLopHocPhan { get; set; }
    }

    public class StudentDetailViewModel
    {
        public int Id { get; set; }
        public string MSSV { get; set; }
        public string HoVaTenDem { get; set; }
        public string Ten { get; set; }
        public int TrangThai { get; set; }
        public DateTime? ThoiGian { get; set; }
    }

}