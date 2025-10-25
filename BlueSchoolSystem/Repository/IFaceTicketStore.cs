namespace BlueSchoolSystem.Repository
{
    public record FaceTicket(string Id, string UserId, DateTimeOffset ExpiresAt);

    public interface IFaceTicketStore
    {
        void Put(FaceTicket t);
        bool Consume(string id, string userId);
    }
}
