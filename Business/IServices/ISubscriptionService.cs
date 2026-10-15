using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Subscription;
using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    public interface ISubscriptionService
    {
        Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync();
        Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(Guid userId);
        Task<List<UserSubscriptionDto>> GetUserSubscriptionHistoryAsync(Guid userId);
        Task<ApiResponse<string>> SubscribeAsync(Guid userId, SubscribeRequest request);
        Task<bool> CancelSubscriptionAsync(Guid userId);
        Task<bool> RenewSubscriptionAsync(Guid userId);
        Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(Guid userId);
        Task HandleSubscriptionChangeAsync(Guid userId, SubscriptionPlan newPlan);

        // Business Logic Methods
        Task<bool> CanUserAccessMachineAsync(Guid userId, Guid machineId);
        Task<bool> CanUserSelectMoreMachinesAsync(Guid userId);
        Task<int> GetRemainingMachineSelectionsAsync(Guid userId);
    }
}
