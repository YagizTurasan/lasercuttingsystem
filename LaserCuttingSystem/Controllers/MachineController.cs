using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Machine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MachineController : ControllerBase
    {
        private readonly IMachineService _machineService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MachineController> _logger;

        public MachineController(
            IMachineService machineService,
            ICurrentUserService currentUser,
            ILogger<MachineController> logger)
        {
            _machineService = machineService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<LaserMachineListDto>>> GetAllMachines()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var machines = await _machineService.GetAllMachinesAsync(userId);
                return Ok(machines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machines for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Makineler listesi alınamadı" });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<LaserMachineDto>> GetMachine(Guid id)
        {
            try
            {
                var userId = _currentUser.UserId;
                var machine = await _machineService.GetMachineByIdAsync(id, userId);

                if (machine == null)
                    return NotFound(new { message = "Makine bulunamadı" });

                return Ok(machine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine {MachineId} for user {UserId}", id, _currentUser.UserId);
                return StatusCode(500, new { message = "Makine detayı alınamadı" });
            }
        }

        [HttpGet("selected")]
        [Authorize]
        public async Task<ActionResult<List<LaserMachineListDto>>> GetSelectedMachines()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var selectedMachines = await _machineService.GetUserSelectedMachinesAsync(userId.Value);

                _logger.LogInformation("User {UserId} has {Count} selected machines", userId, selectedMachines.Count);
                return Ok(selectedMachines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected machines for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Seçili makineler alınamadı" });
            }
        }

        [HttpPost("{id}/select")]
        [Authorize]
        public async Task<ActionResult> SelectMachine(Guid id)
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var result = await _machineService.SelectMachineAsync(userId.Value, id);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserId} selected machine {MachineId}", userId, id);
                    return Ok(new { message = result.Message });
                }
                else
                {
                    _logger.LogWarning("Machine selection failed for user {UserId}, machine {MachineId}: {Message}",
                        userId, id, result.Message);
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting machine {MachineId} for user {UserId}", id, _currentUser.UserId);
                return StatusCode(500, new { message = "Makine seçimi sırasında hata oluştu" });
            }
        }

        [HttpDelete("{id}/unselect")]
        [Authorize]
        public async Task<ActionResult> UnselectMachine(Guid id)
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var success = await _machineService.UnselectMachineAsync(userId.Value, id);

                if (success)
                {
                    _logger.LogInformation("User {UserId} unselected machine {MachineId}", userId, id);
                    return Ok(new { message = "Makine seçimi kaldırıldı" });
                }
                else
                {
                    return BadRequest(new { message = "Makine seçimi bulunamadı" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unselecting machine {MachineId} for user {UserId}", id, _currentUser.UserId);
                return StatusCode(500, new { message = "Makine seçimi kaldırma sırasında hata oluştu" });
            }
        }

        [HttpPut("reorder")]
        [Authorize]
        public async Task<ActionResult> ReorderMachines([FromBody] ReorderRequest request)
        {
            try
            {
                if (!ModelState.IsValid || request.MachineIds == null || !request.MachineIds.Any())
                {
                    return BadRequest(new { message = "Geçersiz sıralama verisi" });
                }

                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var success = await _machineService.ReorderMachineSelectionsAsync(userId.Value, request.MachineIds);

                if (success)
                {
                    _logger.LogInformation("User {UserId} reordered machine selections", userId);
                    return Ok(new { message = "Makine sıralaması güncellendi" });
                }
                else
                {
                    return BadRequest(new { message = "Sıralama güncellenemedi" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering machines for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Sıralama güncelleme sırasında hata oluştu" });
            }
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<LaserMachineListDto>>> SearchMachines([FromQuery] SearchRequest request)
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
                var result = await _machineService.SearchMachinesAsync(request, userId);

                _logger.LogInformation("User {UserId} searched machines with query '{Query}', found {Count} results",
                    userId, request.Query, result.TotalCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching machines for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Arama sırasında hata oluştu" });
            }
        }

        [HttpGet("{id}/models")]
        [Authorize]
        public async Task<ActionResult<List<MachineModelDto>>> GetMachineModels(Guid id)
        {
            try
            {
                var models = await _machineService.GetMachineModelsAsync(id);
                return Ok(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models for machine {MachineId}", id);
                return StatusCode(500, new { message = "Makine modelleri alınamadı" });
            }
        }

        [HttpGet("models/{modelId}/sections")]
        [Authorize]
        public async Task<ActionResult<List<MachineSectionDto>>> GetModelSections(Guid modelId)
        {
            try
            {
                var sections = await _machineService.GetModelSectionsAsync(modelId);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sections for model {ModelId}", modelId);
                return StatusCode(500, new { message = "Model bölümleri alınamadı" });
            }
        }

        [HttpGet("sections/{sectionId}/parts")]
        [Authorize]
        public async Task<ActionResult<List<MachinePartDto>>> GetSectionParts(Guid sectionId)
        {
            try
            {
                var parts = await _machineService.GetSectionPartsAsync(sectionId);
                return Ok(parts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parts for section {SectionId}", sectionId);
                return StatusCode(500, new { message = "Bölüm parçaları alınamadı" });
            }
        }

        [HttpGet("selection-status")]
        [Authorize]
        public async Task<ActionResult> GetSelectionStatus()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var selectedMachines = await _machineService.GetUserSelectedMachinesAsync(userId.Value);
                var subscriptionLevel = _currentUser.GetClaim("subscription_level");
                var subscriptionPlan = _currentUser.GetClaim("subscription_plan");
                var isEnterprise = _currentUser.GetClaim("is_enterprise");

                // Plan limitlerini belirle
                var maxSelections = int.TryParse(subscriptionLevel, out var level) ? level switch
                {
                    0 => 0,  // Free - seçim yapamaz
                    1 => 1,  // Basic - 1 makine
                    2 => 3,  // Professional - 3 makine
                    3 => -1, // Enterprise - sınırsız
                    _ => 0
                } : 0;

                var canSelectMore = bool.TryParse(isEnterprise, out var enterprise) && enterprise
                    ? true
                    : selectedMachines.Count < maxSelections;

                return Ok(new
                {
                    userId = userId.Value,
                    subscription = new
                    {
                        level = subscriptionLevel,
                        plan = subscriptionPlan,
                        isEnterprise = enterprise
                    },
                    selections = new
                    {
                        current = selectedMachines.Count,
                        maximum = maxSelections == -1 ? "Sınırsız" : maxSelections.ToString(),
                        canSelectMore = canSelectMore,
                        remainingSlots = maxSelections == -1 ? -1 : Math.Max(0, maxSelections - selectedMachines.Count)
                    },
                    selectedMachines = selectedMachines.Select(m => new
                    {
                        id = m.Id,
                        brand = m.Brand,
                        selectionOrder = m.SelectionOrder
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selection status for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Seçim durumu alınamadı" });
            }
        }
    }
    public record ReorderRequest
    {
        public List<Guid> MachineIds { get; init; } = new();
    }
}
