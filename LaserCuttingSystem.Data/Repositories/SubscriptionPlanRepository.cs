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
    public class SubscriptionPlanRepository : BaseRepository<SubscriptionPlan>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync()
        {
            return await _dbSet
                .Where(p => p.IsActive)
                .OrderBy(p => p.Level)
                .ToListAsync();
        }

        public async Task<SubscriptionPlan?> GetFreePlanAsync()
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.IsActive && p.Level == 0 && p.Price == 0);
        }
    }
}
