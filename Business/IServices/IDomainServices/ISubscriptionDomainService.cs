using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices.IDomainServices
{
    public interface ISubscriptionDomainService
    {
        Task<bool> ValidateSubscriptionChange(Guid userId, Guid newPlanId);
        Task<DateTime> CalculateSubscriptionEndDate(Guid planId, DateTime startDate);
        Task<decimal> CalculateProrationAmount(Guid userId, Guid newPlanId);
        Task<bool> CanDowngradePlan(Guid userId, Guid newPlanId);
        Task HandleSubscriptionExpiration(Guid subscriptionId);
    }
}
