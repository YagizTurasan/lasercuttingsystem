using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Domain.DTOs.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ICurrentUserService currentUser,
            ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>
        /// Mevcut aboneliklerin planları
        /// </summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<ActionResult<List<SubscriptionPlanDto>>> GetAvailablePlans()
        {
            try
            {
                var plans = await _subscriptionService.GetAvailablePlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription plans");
                return StatusCode(500, new { message = "Planlar alınamadı" });
            }
        }

        /// <summary>
        /// Kullanıcının mevcut aktif aboneliği
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<UserSubscriptionDto>> GetCurrentSubscription()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);

                if (subscription == null)
                    return NotFound(new { message = "Aktif abonelik bulunamadı" });

                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current subscription for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Abonelik bilgileri alınamadı" });
            }
        }

        /// <summary>
        /// Kullanıcının abonelik geçmişi
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<List<UserSubscriptionDto>>> GetSubscriptionHistory()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var history = await _subscriptionService.GetUserSubscriptionHistoryAsync(userId.Value);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription history for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Abonelik geçmişi alınamadı" });
            }
        }

        /// <summary>
        /// Kullanıcının ödeme geçmişi
        /// </summary>
        [HttpGet("payments")]
        public async Task<ActionResult<List<PaymentHistoryDto>>> GetPaymentHistory()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var payments = await _subscriptionService.GetPaymentHistoryAsync(userId.Value);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Ödeme geçmişi alınamadı" });
            }
        }

        /// <summary>
        /// DEMO: Yeni abonelik satın al - Anında aktif
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<ActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz abonelik verisi",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var result = await _subscriptionService.SubscribeAsync(userId.Value, request);

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                return Ok(new
                {
                    message = result.Message,
                    subscriptionId = result.Data,
                    requiresPayment = false, // Demo modunda ödeme yok
                    isDemo = true,
                    note = "Bu demo uygulamasıdır - gerçek ödeme yapılmaz",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing user {UserId} to plan {PlanId}",
                    _currentUser.UserId, request.PlanId);
                return StatusCode(500, new { message = "Abonelik oluşturma sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Aboneliği iptal et
        /// </summary>
        [HttpPost("cancel")]
        public async Task<ActionResult> CancelSubscription()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var success = await _subscriptionService.CancelSubscriptionAsync(userId.Value);

                if (success)
                {
                    _logger.LogInformation("DEMO: User {UserId} cancelled subscription", userId);
                    return Ok(new
                    {
                        message = "Abonelik iptal edildi",
                        isDemo = true
                    });
                }
                else
                {
                    return BadRequest(new { message = "İptal edilecek aktif abonelik bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Abonelik iptali sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Aboneliği yenile (Demo)
        /// </summary>
        [HttpPost("renew")]
        public async Task<ActionResult> RenewSubscription()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var success = await _subscriptionService.RenewSubscriptionAsync(userId.Value);

                if (success)
                {
                    _logger.LogInformation("DEMO: User {UserId} renewed subscription", userId);
                    return Ok(new
                    {
                        message = "Abonelik yenilendi",
                        isDemo = true,
                        note = "Demo modunda otomatik yenileme"
                    });
                }
                else
                {
                    return BadRequest(new { message = "Yenilenecek uygun abonelik bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing subscription for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Abonelik yenileme sırasında hata oluştu" });
            }
        }

        /// <summary>
        /// Kullanıcının makine seçim durumu ve kalan hakları
        /// </summary>
        [HttpGet("machine-selection-info")]
        public async Task<ActionResult> GetMachineSelectionInfo()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var canSelectMore = await _subscriptionService.CanUserSelectMoreMachinesAsync(userId.Value);
                var remainingSelections = await _subscriptionService.GetRemainingMachineSelectionsAsync(userId.Value);

                var subscriptionLevel = _currentUser.GetClaim("subscription_level");
                var subscriptionPlan = _currentUser.GetClaim("subscription_plan");
                var isEnterprise = _currentUser.GetClaim("is_enterprise");

                return Ok(new
                {
                    userId = userId.Value,
                    subscription = new
                    {
                        level = subscriptionLevel,
                        plan = subscriptionPlan,
                        isEnterprise = bool.TryParse(isEnterprise, out var enterprise) ? enterprise : false
                    },
                    machineSelection = new
                    {
                        canSelectMore = canSelectMore,
                        remainingSelections = remainingSelections == int.MaxValue ? -1 : remainingSelections,
                        remainingText = remainingSelections switch
                        {
                            -1 => "Sınırsız",
                            int.MaxValue => "Sınırsız",
                            0 => "Seçim hakkınız yok",
                            1 => "1 seçim hakkınız var",
                            _ => $"{remainingSelections} seçim hakkınız var"
                        }
                    },
                    isDemo = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine selection info for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Makine seçim bilgileri alınamadı" });
            }
        }

        /// <summary>
        /// Abonelik özeti - kullanıcı dashboard için
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult> GetSubscriptionSummary()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var currentSubscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
                var canSelectMore = await _subscriptionService.CanUserSelectMoreMachinesAsync(userId.Value);
                var remainingSelections = await _subscriptionService.GetRemainingMachineSelectionsAsync(userId.Value);

                return Ok(new
                {
                    subscription = currentSubscription != null ? new
                    {
                        planName = currentSubscription.Plan?.DisplayName,
                        level = currentSubscription.Plan?.Level,
                        price = currentSubscription.Plan?.Price,
                        currency = currentSubscription.Plan?.Currency,
                        startDate = currentSubscription.StartDate,
                        endDate = currentSubscription.EndDate,
                        daysRemaining = currentSubscription.DaysRemaining,
                        isActive = currentSubscription.IsCurrentlyActive,
                        autoRenew = currentSubscription.AutoRenew
                    } : null,
                    capabilities = new
                    {
                        canSelectMachines = canSelectMore || remainingSelections > 0,
                        remainingMachineSelections = remainingSelections == int.MaxValue ? -1 : remainingSelections,
                        hasUnlimitedAccess = currentSubscription?.Plan?.CanAccessAllMachines ?? false,
                        maxMachineSelections = currentSubscription?.Plan?.MaxMachineSelection ?? 0
                    },
                    isDemo = true,
                    demoNote = "Demo modunda çalışmaktadır"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription summary for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Abonelik özeti alınamadı" });
            }
        }

        /// <summary>
        /// Plan karşılaştırması
        /// </summary>
        [HttpGet("compare")]
        [AllowAnonymous]
        public async Task<ActionResult> ComparePlans()
        {
            try
            {
                var plans = await _subscriptionService.GetAvailablePlansAsync();

                var comparison = plans.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    displayName = p.DisplayName,
                    price = p.Price,
                    currency = p.Currency,
                    level = p.Level,
                    features = new
                    {
                        machineSelection = p.MaxMachineSelection == -1 ? "Sınırsız" :
                                          p.MaxMachineSelection == 0 ? "Yok" :
                                          $"{p.MaxMachineSelection} makine",
                        errorCodeAccess = p.Level == 0 ? "Sadece ücretsiz kodlar" :
                                         p.CanAccessAllMachines ? "Tüm hata kodları" :
                                         "Seçili makinelerin tüm kodları + ücretsiz kodlar",
                        fullAccess = p.CanAccessAllMachines,
                        isFreePlan = p.IsFreePlan
                    },
                    demoNote = p.Price > 0 ? "Demo modunda gerçek ödeme yapılmaz" : null
                }).OrderBy(p => p.level).ToList();

                return Ok(new
                {
                    plans = comparison,
                    isDemo = true,
                    message = "Demo modunda tüm planlar anında aktif olur"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing plans");
                return StatusCode(500, new { message = "Plan karşılaştırması alınamadı" });
            }
        }
    }
}