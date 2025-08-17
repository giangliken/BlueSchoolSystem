namespace BlueSchoolSystem.Models.ViewModel
{
    public class XacNhanOtpResponse
    {
        //[JsonPropertyName("result")]
        public bool result { get; set; }

        //[JsonPropertyName("code")]
        public int code { get; set; }

        //[JsonPropertyName("resetToken")]
        public string? resetToken { get; set; }

        //[JsonPropertyName("message")]
        public string? message { get; set; }
    }
}
