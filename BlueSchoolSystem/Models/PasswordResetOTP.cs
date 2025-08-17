namespace BlueSchoolSystem.Models
{
    public class PasswordResetOTP
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string OTPCode { get; set; }

        public DateTime ExpireAt { get; set; }
    }
}
