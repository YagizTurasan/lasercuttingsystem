using LaserCuttingSystem.Domain.Entities.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IUserActivityRepository : IRepository<UserActivity>
    {
        Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task LogActivityAsync(Guid? userId, string activityType, string? entityId = null, string? ipAddress = null);
    }
}
