namespace BlueSchoolSystem.Models.ViewModel
{
    public class CheckinDiemDanhRequest
    {
        public int DiemDanhId { get; set; }
        public string Code { get; set; }
        public string? DeviceId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

}
