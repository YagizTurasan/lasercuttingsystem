using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface ISubscriptionPlanRepository : IRepository<SubscriptionPlan>
    {
        Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync();
        Task<SubscriptionPlan?> GetFreePlanAsync();
    }
}
