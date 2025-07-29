
using BlueSchoolSystem.Models;

namespace BlueSchoolSystem.Repository
{
    public class EFActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        public EFActivityLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<IEnumerable<ActivityLog>> GetAllLogs()
        {
            var logs = _context.ActivityLogs
                .OrderByDescending(log => log.Timestamp)
                .AsEnumerable();
            return Task.FromResult(logs);
        }

        public async Task LogAsync(string userId, string userName, string actionType, string tableName, string objectId, string description)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                ActionType = actionType,
                TableName = tableName,
                ObjectId = objectId,
                Description = description,
                Timestamp = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
