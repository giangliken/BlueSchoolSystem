namespace BlueSchoolSystem.Models
{
    public class UserFaceTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = default!;        
        public byte[] Embedding { get; set; } = Array.Empty<byte>(); 
        public string Model { get; set; } = "MobileFaceNet-112x112";
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ApplicationUser User { get; set; } = default!;
    }

    public class FaceVerifyLog
    {
        public long Id { get; set; }
        public string UserId { get; set; } = default!;
        public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
        public float Score { get; set; }
        public bool Success { get; set; }
        public string? DeviceId { get; set; }
        public string? Ip { get; set; }
    }
}
