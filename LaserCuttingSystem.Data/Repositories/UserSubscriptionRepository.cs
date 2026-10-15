using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class UserSubscriptionRepository : BaseRepository<UserSubscription>, IUserSubscriptionRepository
    {
        public UserSubscriptionRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<UserSubscription?> GetActiveSubscriptionByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId &&
                    s.IsActive &&
                    s.Status == Domain.Enums.SubscriptionStatus.Active &&
                    s.EndDate > DateTime.UtcNow);
        }

        public async Task<IEnumerable<UserSubscription>> GetUserSubscriptionHistoryAsync(Guid userId)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }
    }

}
