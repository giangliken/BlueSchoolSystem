using Microsoft.Extensions.Caching.Memory;

namespace BlueSchoolSystem.Repository
{
    public class EFMemoryFaceTicketStore: IFaceTicketStore
    {
        private readonly IMemoryCache _cache;
        public EFMemoryFaceTicketStore(IMemoryCache cache) => _cache = cache;

        public void Put(FaceTicket t) => _cache.Set(t.Id, t, t.ExpiresAt);

        public bool Consume(string id, string userId)
        {
            if (_cache.TryGetValue(id, out FaceTicket? t) && t!.UserId == userId)
            {
                _cache.Remove(id);
                return true;
            }
            return false;
        }
    }
}
