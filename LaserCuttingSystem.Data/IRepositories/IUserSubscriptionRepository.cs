using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IUserSubscriptionRepository : IRepository<UserSubscription>
    {
        Task<UserSubscription?> GetActiveSubscriptionByUserAsync(Guid userId);
        Task<IEnumerable<UserSubscription>> GetUserSubscriptionHistoryAsync(Guid userId);
    }
}
