using LaserCuttingSystem.Business.IServices.IDomainServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services.DomainService
{
    public class SubscriptionDomainService : ISubscriptionDomainService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubscriptionDomainService> _logger;

        public SubscriptionDomainService(
            IUnitOfWork unitOfWork,
            ILogger<SubscriptionDomainService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> ValidateSubscriptionChange(Guid userId, Guid newPlanId)
        {
            try
            {
                // Yeni planı al
                var newPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(newPlanId);
                if (newPlan == null || !newPlan.IsActive)
                {
                    _logger.LogWarning("Invalid or inactive plan {PlanId} for user {UserId}", newPlanId, userId);
                    return false;
                }

                // Mevcut subscription'ı al
                var currentSubscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                if (currentSubscription == null)
                {
                    // İlk subscription - her zaman geçerli (free'den herhangi bir plana)
                    _logger.LogDebug("First subscription for user {UserId} to plan {PlanId}", userId, newPlanId);
                    return true;
                }

                var currentPlan = currentSubscription.Plan;

                // Aynı plana geçiş yapılamaz
                if (currentPlan.Id == newPlanId)
                {
                    _logger.LogDebug("User {UserId} trying to change to same plan {PlanId}", userId, newPlanId);
                    return false;
                }

                // Free planından herhangi bir plana geçiş mümkün
                if (currentPlan.Level == 0)
                {
                    _logger.LogDebug("User {UserId} upgrading from free plan to {PlanLevel}", userId, newPlan.Level);
                    return true;
                }

                // Downgrade kontrolü
                if (newPlan.Level < currentPlan.Level)
                {
                    var canDowngrade = await CanDowngradePlan(userId, newPlanId);
                    _logger.LogDebug("User {UserId} downgrade validation from {CurrentLevel} to {NewLevel}: {CanDowngrade}",
                        userId, currentPlan.Level, newPlan.Level, canDowngrade);
                    return canDowngrade;
                }

                // Upgrade her zaman mümkün
                _logger.LogDebug("User {UserId} upgrading from {CurrentLevel} to {NewLevel}",
                    userId, currentPlan.Level, newPlan.Level);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating subscription change for user {UserId} to plan {PlanId}",
                    userId, newPlanId);
                return false;
            }
        }

        public async Task<DateTime> CalculateSubscriptionEndDate(Guid planId, DateTime startDate)
        {
            try
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(planId);
                if (plan == null)
                {
                    _logger.LogWarning("Plan {PlanId} not found for end date calculation", planId);
                    return startDate.AddMonths(1); // Default to monthly
                }

                var endDate = plan.BillingPeriod switch
                {
                    BillingPeriod.Monthly => startDate.AddMonths(1),
                    BillingPeriod.Yearly => startDate.AddYears(1),
                    _ => startDate.AddMonths(1)
                };

                _logger.LogDebug("Plan {PlanId} end date calculated: {StartDate} -> {EndDate} ({BillingPeriod})",
                    planId, startDate, endDate, plan.BillingPeriod);

                return endDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating end date for plan {PlanId}", planId);
                return startDate.AddMonths(1);
            }
        }

        public async Task<decimal> CalculateProrationAmount(Guid userId, Guid newPlanId)
        {
            try
            {
                var currentSubscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                if (currentSubscription == null)
                {
                    _logger.LogDebug("No current subscription for user {UserId}, no proration needed", userId);
                    return 0m;
                }

                var newPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(newPlanId);
                if (newPlan == null)
                {
                    _logger.LogWarning("New plan {PlanId} not found for proration calculation", newPlanId);
                    return 0m;
                }

                var currentPlan = currentSubscription.Plan;

                // Free planından upgrade: Full amount
                if (currentPlan.Level == 0)
                {
                    _logger.LogDebug("Upgrading from free plan, full amount: {Amount}", newPlan.Price);
                    return newPlan.Price;
                }

                // Aynı billing period kontrolü
                if (currentPlan.BillingPeriod != newPlan.BillingPeriod)
                {
                    // Farklı billing period'larda proration karmaşık, full amount al
                    _logger.LogDebug("Different billing periods, charging full amount: {Amount}", newPlan.Price);
                    return newPlan.Price;
                }

                // Kalan gün sayısını hesapla
                var now = DateTime.UtcNow;
                var remainingDays = (currentSubscription.EndDate - now).TotalDays;

                if (remainingDays <= 0)
                {
                    _logger.LogDebug("Subscription expired, charging full amount: {Amount}", newPlan.Price);
                    return newPlan.Price;
                }

                // Total gün sayısını hesapla
                var totalDays = (currentSubscription.EndDate - currentSubscription.StartDate).TotalDays;
                var usedRatio = 1 - (remainingDays / totalDays);

                // Current plan'dan kullanılan kısmı hesapla
                var usedAmount = currentPlan.Price * (decimal)usedRatio;

                // New plan'ın tam fiyatından kullanılmayan kısmı çıkar
                var proratedAmount = newPlan.Price - (currentPlan.Price - usedAmount);

                // Negatif ise 0 yap (downgrade durumu)
                proratedAmount = Math.Max(0, proratedAmount);

                _logger.LogDebug("Proration calculated for user {UserId}: Current={CurrentPrice}, New={NewPrice}, " +
                    "RemainingDays={RemainingDays}, TotalDays={TotalDays}, ProratedAmount={ProratedAmount}",
                    userId, currentPlan.Price, newPlan.Price, remainingDays, totalDays, proratedAmount);

                return proratedAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating proration for user {UserId} to plan {PlanId}",
                    userId, newPlanId);
                return 0m;
            }
        }

        public async Task<bool> CanDowngradePlan(Guid userId, Guid newPlanId)
        {
            try
            {
                var newPlan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(newPlanId);
                if (newPlan == null)
                {
                    _logger.LogWarning("New plan {PlanId} not found for downgrade validation", newPlanId);
                    return false;
                }

                // Free plana downgrade her zaman mümkün
                if (newPlan.Level == 0)
                {
                    _logger.LogDebug("Downgrading to free plan is always allowed for user {UserId}", userId);
                    return true;
                }

                // Enterprise'dan diğer planlara: Makine seçimi kontrolü
                if (newPlan.Level < 3) // Enterprise'dan downgrade
                {
                    var currentSelectionCount = await _unitOfWork.UserMachineSelections
                        .GetUserSelectionCountAsync(userId);

                    // Unlimited selection'dan limited'a geçiş
                    if (newPlan.MaxMachineSelection != -1 && currentSelectionCount > newPlan.MaxMachineSelection)
                    {
                        _logger.LogWarning("User {UserId} has {CurrentSelections} machines selected, " +
                            "but new plan {PlanId} allows only {MaxSelections}",
                            userId, currentSelectionCount, newPlanId, newPlan.MaxMachineSelection);
                        return false;
                    }
                }

                // Professional'dan Basic'e: 3'ten 1'e düşüyor
                if (newPlan.Level == 1) // Basic plan
                {
                    var currentSelectionCount = await _unitOfWork.UserMachineSelections
                        .GetUserSelectionCountAsync(userId);

                    if (currentSelectionCount > 1)
                    {
                        _logger.LogWarning("User {UserId} has {CurrentSelections} machines selected, " +
                            "but Basic plan allows only 1", userId, currentSelectionCount);
                        return false;
                    }
                }

                _logger.LogDebug("Downgrade validation passed for user {UserId} to plan {PlanId}", userId, newPlanId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating downgrade for user {UserId} to plan {PlanId}",
                    userId, newPlanId);
                return false;
            }
        }

        public async Task HandleSubscriptionExpiration(Guid subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.UserSubscriptions.GetByIdAsync(subscriptionId,
                    s => s.User, s => s.Plan);

                if (subscription == null)
                {
                    _logger.LogWarning("Subscription {SubscriptionId} not found for expiration handling", subscriptionId);
                    return;
                }

                _logger.LogInformation("Handling subscription expiration for user {UserId}, subscription {SubscriptionId}",
                    subscription.UserId, subscriptionId);

                // Subscription'ı expired yap
                subscription.Status = SubscriptionStatus.Expired;
                subscription.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.UserSubscriptions.Update(subscription);

                // AutoRenew kontrolü
                if (subscription.AutoRenew && subscription.Plan.Level > 0)
                {
                    _logger.LogDebug("Attempting auto-renewal for subscription {SubscriptionId}", subscriptionId);

                    // Auto-renewal logic burada olabilir
                    // Örneğin: payment service'e auto-payment request gönder
                    // Şimdilik sadece log
                    _logger.LogInformation("Auto-renewal would be triggered for subscription {SubscriptionId}", subscriptionId);
                }

                // Eğer kullanıcı paid plan'dan free'ye düşüyorsa, makine seçimlerini pasif yap
                if (subscription.Plan.Level > 0)
                {
                    await DeactivateUserMachineSelections(subscription.UserId);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Subscription expiration handled for user {UserId}", subscription.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling subscription expiration for subscription {SubscriptionId}",
                    subscriptionId);
                throw; // Re-throw to ensure calling code knows about the failure
            }
        }

        // Private helper methods

        private async Task DeactivateUserMachineSelections(Guid userId)
        {
            try
            {
                var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);

                foreach (var selection in selections)
                {
                    selection.IsActive = false;
                    selection.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.UserMachineSelections.Update(selection);
                }

                _logger.LogInformation("Deactivated {Count} machine selections for user {UserId}",
                    selections.Count(), userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating machine selections for user {UserId}", userId);
                throw;
            }
        }
    }
}
