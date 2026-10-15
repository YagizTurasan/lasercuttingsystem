using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Analytics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class UserActivityRepository : BaseRepository<UserActivity>, IUserActivityRepository
    {
        public UserActivityRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(Guid userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _dbSet.Where(a => a.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task LogActivityAsync(Guid? userId, string activityType, string? entityId = null, string? ipAddress = null)
        {
            var activity = new UserActivity
            {
                UserId = userId,
                ActivityType = activityType,
                EntityId = entityId,
                IpAddress = ipAddress
            };

            await AddAsync(activity);
        }
    }
}
