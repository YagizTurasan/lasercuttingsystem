using AutoMapper;
using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Payment;
using LaserCuttingSystem.Domain.DTOs.Subscription;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Entities.Subscription;
using LaserCuttingSystem.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly IPaymentGateway _paymentGateway;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IEmailService emailService,
            IPaymentGateway paymentGateway,
            UserManager<ApplicationUser> userManager,
            ILogger<SubscriptionService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailService = emailService;
            _paymentGateway = paymentGateway;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<SubscriptionPlanDto>> GetAvailablePlansAsync()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetActivePlansAsync();
            return _mapper.Map<List<SubscriptionPlanDto>>(plans);
        }

        public async Task<UserSubscriptionDto?> GetActiveSubscriptionAsync(Guid userId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            return subscription != null ? _mapper.Map<UserSubscriptionDto>(subscription) : null;
        }

        public async Task<List<UserSubscriptionDto>> GetUserSubscriptionHistoryAsync(Guid userId)
        {
            var subscriptions = await _unitOfWork.UserSubscriptions.GetUserSubscriptionHistoryAsync(userId);
            return _mapper.Map<List<UserSubscriptionDto>>(subscriptions);
        }

        public async Task<ApiResponse<string>> SubscribeAsync(Guid userId, SubscribeRequest request)
        {
            var strategy = _unitOfWork.Context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async (cancellationToken) =>
            {
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Plan kontrolü
                    var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(request.PlanId);
                    if (plan == null || !plan.IsActive)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = "Plan bulunamadı",
                            Errors = new List<string> { "Seçilen plan mevcut değil." }
                        };
                    }

                    // Aktif abonelik kontrolü
                    var currentSubscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                    if (currentSubscription != null)
                    {
                        if (currentSubscription.PlanId == request.PlanId)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            return new ApiResponse<string>
                            {
                                Success = false,
                                Message = "Zaten bu plana sahipsiniz",
                                Errors = new List<string> { "Bu plana zaten sahipsiniz." }
                            };
                        }

                        // Mevcut aboneliği iptal et
                        currentSubscription.Cancel();
                        _unitOfWork.UserSubscriptions.Update(currentSubscription);
                    }

                    // Yeni abonelik oluştur
                    var subscription = new UserSubscription
                    {
                        UserId = userId,
                        PlanId = plan.Id,
                        StartDate = DateTime.UtcNow,
                        EndDate = CalculateEndDate(plan.BillingPeriod),
                        AutoRenew = request.AutoRenew,
                        Status = SubscriptionStatus.Active
                    };

                    await _unitOfWork.UserSubscriptions.AddAsync(subscription);
                    var payment = await ChargeAndRecordPaymentAsync(userId, subscription.Id, plan);
                    if (payment is { Success: false })
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        return new ApiResponse<string>
                        {
                            Success = false,
                            Message = "Ödeme alınamadı",
                            Errors = new List<string> { payment.ErrorMessage ?? "Ödeme sağlayıcısı işlemi reddetti." }
                        };
                    }
                    await _unitOfWork.SaveChangesAsync();

                    // ÖNEMLİ: Yeni abonelik sonrası makine seçimlerini düzenle
                    await HandleSubscriptionChangeAsync(userId, plan);

                    await _unitOfWork.CommitTransactionAsync();

                    _logger.LogInformation("Successfully created subscription {SubscriptionId} for user {UserId}, Plan: {PlanName}",
                        subscription.Id, userId, plan.DisplayName);

                    // Email gönder (opsiyonel)
                    try
                    {
                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user?.Email != null)
                        {
                            await _emailService.SendSubscriptionConfirmationAsync(
                                user.Email, plan.DisplayName, plan.Price);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Email sending failed for subscription {SubscriptionId}", subscription.Id);
                    }

                    return new ApiResponse<string>
                    {
                        Success = true,
                        Message = plan.Price == 0
                            ? $"Ücretsiz {plan.DisplayName} planına başarıyla geçtiniz"
                            : $"DEMO: {plan.DisplayName} planı aktive edildi (Gerçek ödeme yapılmadı)",
                        Data = subscription.Id.ToString()
                    };
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);

                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Abonelik oluşturulurken bir hata oluştu",
                        Errors = new List<string> { ex.Message }
                    };
                }
            }, CancellationToken.None);
        }

        public async Task<bool> CancelSubscriptionAsync(Guid userId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            if (subscription == null) return false;

            subscription.Cancel();
            _unitOfWork.UserSubscriptions.Update(subscription);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("DEMO: Cancelled subscription for user {UserId}", userId);
            return true;
        }

        public async Task<bool> RenewSubscriptionAsync(Guid userId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            if (subscription == null || !subscription.AutoRenew) return false;

            // Önce ödeme alınır; başarısızsa abonelik uzatılmaz
            var payment = await ChargeAndRecordPaymentAsync(userId, subscription.Id, subscription.Plan);
            if (payment is { Success: false })
            {
                _logger.LogWarning("Renewal payment failed for user {UserId}: {Error}", userId, payment.ErrorMessage);
                return false;
            }

            var newEndDate = CalculateEndDate(subscription.Plan.BillingPeriod, subscription.EndDate);
            subscription.Renew(newEndDate);

            _unitOfWork.UserSubscriptions.Update(subscription);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Renewed subscription for user {UserId}", userId);
            return true;
        }

        public async Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(Guid userId)
        {
            var payments = await _unitOfWork.PaymentHistories.GetUserPaymentHistoryAsync(userId);
            return _mapper.Map<List<PaymentHistoryDto>>(payments);
        }

        // CORE BUSINESS LOGIC - Değişmeden kalacak
        public async Task<bool> CanUserAccessMachineAsync(Guid userId, Guid machineId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            if (subscription == null) return false;

            if (subscription.Plan.CanAccessAllMachines) return true;

            var isSelected = await _unitOfWork.UserMachineSelections.IsUserSelectedMachineAsync(userId, machineId);
            return isSelected;
        }

        public async Task<bool> CanUserSelectMoreMachinesAsync(Guid userId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            if (subscription == null) return false;

            if (subscription.Plan.MaxMachineSelection == 0) return false;
            if (subscription.Plan.MaxMachineSelection == -1) return true;

            var currentSelectionCount = await _unitOfWork.UserMachineSelections.GetUserSelectionCountAsync(userId);
            return currentSelectionCount < subscription.Plan.MaxMachineSelection;
        }

        public async Task<int> GetRemainingMachineSelectionsAsync(Guid userId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            if (subscription == null) return 0;

            if (subscription.Plan.CanAccessAllMachines) return -1;
            if (subscription.Plan.MaxMachineSelection == 0) return 0;
            if (subscription.Plan.MaxMachineSelection == -1) return int.MaxValue;

            var currentSelectionCount = await _unitOfWork.UserMachineSelections.GetUserSelectionCountAsync(userId);
            return Math.Max(0, subscription.Plan.MaxMachineSelection - currentSelectionCount);
        }

        public async Task<bool> HandlePaymentSuccessAsync(Guid subscriptionId, string paymentId)
        {
            _logger.LogInformation("DEMO: HandlePaymentSuccessAsync called but not needed - subscriptions are created as Active");
            return true; // Demo modunda her zaman true döndür
        }

        public async Task HandleSubscriptionChangeAsync(Guid userId, SubscriptionPlan newPlan)
        {
            _logger.LogInformation("Handling subscription change for user {UserId}, new plan: {PlanName}",
         userId, newPlan.DisplayName);

            // Tüm aktif makine seçimlerini iptal et
            var allSelections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
            var allIds = allSelections.Select(s => s.Id).ToList();

            if (allIds.Any())
            {
                await _unitOfWork.UserMachineSelections.DeactivateUserSelectionsAsync(allIds);
                _logger.LogInformation("All previous machine selections have been deactivated for user {UserId}", userId);
            }

            // Plan unlimited değilse logla
            if (!newPlan.CanAccessAllMachines && newPlan.MaxMachineSelection != -1)
            {
                _logger.LogInformation("User {UserId} can now select up to {Max} machines", userId, newPlan.MaxMachineSelection);
            }
        }

        // Ücretli planlarda ödeme sağlayıcısından tahsilat yapılır ve sonuç ödeme geçmişine yazılır.
        // Ücretsiz planda ödeme alınmaz (null döner).
        private async Task<PaymentResult?> ChargeAndRecordPaymentAsync(Guid userId, Guid subscriptionId, SubscriptionPlan plan)
        {
            if (plan.Price <= 0) return null;

            var result = await _paymentGateway.ChargeAsync(new PaymentRequest
            {
                Amount = plan.Price,
                Currency = plan.Currency,
                Metadata = new Dictionary<string, object>
                {
                    ["userId"] = userId,
                    ["subscriptionId"] = subscriptionId,
                    ["plan"] = plan.Name
                }
            });

            if (result.Success)
            {
                await _unitOfWork.PaymentHistories.AddAsync(new PaymentHistory
                {
                    UserId = userId,
                    SubscriptionId = subscriptionId,
                    Amount = plan.Price,
                    Currency = plan.Currency,
                    Status = result.Status,
                    TransactionDate = DateTime.UtcNow,
                    PaymentProviderId = result.PaymentId
                });
            }

            return result;
        }

        private static DateTime CalculateEndDate(BillingPeriod billingPeriod, DateTime? startDate = null)
        {
            var start = startDate ?? DateTime.UtcNow;
            return billingPeriod switch
            {
                BillingPeriod.Monthly => start.AddMonths(1),
                BillingPeriod.Yearly => start.AddYears(1),
                _ => start.AddMonths(1)
            };
        }
    }
}