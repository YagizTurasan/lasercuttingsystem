using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Error;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ErrorCodesController : ControllerBase
    {
        private readonly IErrorCodeService _errorCodeService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ErrorCodesController> _logger;

        public ErrorCodesController(
            IErrorCodeService errorCodeService,
            ICurrentUserService currentUser,
            ILogger<ErrorCodesController> logger)
        {
            _errorCodeService = errorCodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet("free")]
        public async Task<ActionResult<List<ErrorCodeListDto>>> GetFreeErrorCodes()
        {
            try
            {
                var errorCodes = await _errorCodeService.GetFreeErrorCodesAsync();
                return Ok(errorCodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting free error codes");
                return StatusCode(500, new { message = "Hata kodları alınamadı" });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ErrorCodeDto>> GetErrorCode(Guid id)
        {
            try
            {
                var userId = _currentUser.UserId;
                var errorCode = await _errorCodeService.GetErrorCodeByIdAsync(id, userId);

                if (errorCode == null)
                    return NotFound(new { message = "Hata kodu bulunamadı veya erişim yetkiniz yok" });

                return Ok(errorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error code {ErrorCodeId} for user {UserId}", id, _currentUser.UserId);
                return StatusCode(500, new { message = "Hata kodu alınamadı" });
            }
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<ErrorCodeListDto>>> SearchErrorCodes([FromQuery] SearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz arama parametreleri",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var userId = _currentUser.UserId;
                var result = await _errorCodeService.SearchErrorCodesAsync(request, userId);

                _logger.LogInformation("User {UserId} searched error codes with query '{Query}', found {Count} results",
                    userId, request.Query, result.TotalCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching error codes for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Arama sırasında hata oluştu" });
            }
        }

        // Frontend aramayı JSON body ile gönderir; Filters sözlüğü query string'den güvenilir şekilde bağlanmadığı için POST da desteklenir
        [HttpPost("search")]
        [Authorize]
        public Task<ActionResult<PagedResult<ErrorCodeListDto>>> SearchErrorCodesByBody([FromBody] SearchRequest request)
            => SearchErrorCodes(request);

        [HttpGet("by-machine/{machineId}")]
        [Authorize]
        public async Task<ActionResult<List<ErrorCodeListDto>>> GetErrorCodesByMachine(Guid machineId)
        {
            try
            {
                var userId = _currentUser.UserId;
                var errorCodes = await _errorCodeService.GetErrorCodesByMachineAsync(machineId, userId);

                _logger.LogInformation("User {UserId} accessed error codes for machine {MachineId}, found {Count} codes",
                    userId, machineId, errorCodes.Count);

                return Ok(errorCodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error codes for machine {MachineId} and user {UserId}",
                    machineId, _currentUser.UserId);
                return StatusCode(500, new { message = "Makine hata kodları alınamadı" });
            }
        }

        [HttpGet("sources")]
        public async Task<ActionResult<List<ErrorSourceDto>>> GetErrorSources()
        {
            try
            {
                var sources = await _errorCodeService.GetErrorSourcesAsync();
                return Ok(sources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error sources");
                return StatusCode(500, new { message = "Hata kaynakları alınamadı" });
            }
        }

        [HttpGet("access-info")]
        [Authorize]
        public async Task<ActionResult> GetAccessInfo()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var subscriptionLevel = _currentUser.GetClaim("subscription_level");
                var subscriptionPlan = _currentUser.GetClaim("subscription_plan");
                var isEnterprise = _currentUser.GetClaim("is_enterprise");

                // Erişilebilir hata kodları sayısını al
                var accessibleCodes = await _errorCodeService.GetAccessibleErrorCodesAsync(userId);
                var freeCodes = await _errorCodeService.GetFreeErrorCodesAsync();

                return Ok(new
                {
                    userId = userId.Value,
                    subscription = new
                    {
                        level = subscriptionLevel,
                        plan = subscriptionPlan,
                        isEnterprise = bool.TryParse(isEnterprise, out var enterprise) ? enterprise : false
                    },
                    access = new
                    {
                        totalAccessibleCodes = accessibleCodes.Count,
                        freeCodesCount = freeCodes.Count,
                        premiumCodesCount = accessibleCodes.Count - freeCodes.Count,
                        hasFullAccess = bool.TryParse(isEnterprise, out var fullAccess) ? fullAccess : false
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access info for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Erişim bilgileri alınamadı" });
            }
        }

        [HttpGet("stats")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> GetErrorCodeStats()
        {
            try
            {
                var allCodes = await _errorCodeService.GetAccessibleErrorCodesAsync(null);
                var freeCodes = await _errorCodeService.GetFreeErrorCodesAsync();

                var stats = new
                {
                    totalErrorCodes = allCodes.Count,
                    freeErrorCodes = freeCodes.Count,
                    premiumErrorCodes = allCodes.Count - freeCodes.Count,
                    errorSources = (await _errorCodeService.GetErrorSourcesAsync()).Count,
                    breakdown = allCodes
                        .GroupBy(e => e.AccessLevel)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting error code statistics");
                return StatusCode(500, new { message = "İstatistikler alınamadı" });
            }
        }

    }
}