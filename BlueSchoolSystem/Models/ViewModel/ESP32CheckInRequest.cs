namespace BlueSchoolSystem.Models.ViewModel
{
    public class ESP32CheckInRequest
    {
        public string DeviceId { get; set; } // Mã định danh của mạch ESP (VD: PHONG_A1)
        public List<string> ScannedNames { get; set; } // Danh sách tên bluetooth quét được (MSSV)
    }
}
