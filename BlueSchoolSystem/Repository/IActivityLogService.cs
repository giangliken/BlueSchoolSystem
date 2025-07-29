using BlueSchoolSystem.Models;

namespace BlueSchoolSystem
{
    public interface IActivityLogService
    {
         Task LogAsync(string userId, string userName, string actionType, string tableName, string objectId, string description);
         Task<IEnumerable<ActivityLog>> GetAllLogs();
    }
}
