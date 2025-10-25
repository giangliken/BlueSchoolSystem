namespace BlueSchoolSystem.Models.ViewModel
{
    public record FaceRegisterRequest(List<float[]> Embeddings, string Model);
    public record FaceRegisterResponse(bool Success);

    public record FaceVerifyRequest(float[] Embedding, string Model);
    public record FaceVerifyResponse(bool Match, float Score, string? TicketId = null, int? TtlSec = null);

    public class FaceVerifyOptions
    {
        public float Threshold { get; set; } = 0.35f; 
        public int TicketTtlSeconds { get; set; } = 180;
        public int MaxTemplatesPerUser { get; set; } = 5; 
    }
}
