using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Domain.DTOs.Admin.Machines;
using LaserCuttingSystem.Domain.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/machines")]
    [Authorize(Roles = "Admin")]
    public class AdminMachineController : ControllerBase
    {
        private readonly IAdminMachineService _machineService;

        public AdminMachineController(IAdminMachineService machineService)
        {
            _machineService = machineService;
        }

        
        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminMachineListDto>>>> GetMachines(
            [FromBody] SearchRequest request)
        {
            try
            {
                var machines = await _machineService.GetMachinesAsync(request);
                return Ok(ApiResponse<PagedResult<AdminMachineListDto>>.CreateSuccess(machines));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<AdminMachineListDto>>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateMachine([FromBody] AdminMachineCreateRequest request)
        {
            try
            {
                var result = await _machineService.CreateMachineAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetMachines), result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

       
        [HttpPut("{machineId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateMachine(
            Guid machineId,
            [FromBody] AdminMachineUpdateRequest request)
        {
            try
            {
                var result = await _machineService.UpdateMachineAsync(machineId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("{machineId:guid}")]
        
        public async Task<ActionResult<ApiResponse<string>>> DeleteMachine(Guid machineId)
        {
            try
            {
                var result = await _machineService.DeleteMachineAsync(machineId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        

       
        [HttpPost("{machineId:guid}/models")]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateMachineModel(
            Guid machineId,
            [FromBody] AdminMachineModelRequest request)
        {
            try
            {
                var result = await _machineService.CreateMachineModelAsync(machineId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPut("models/{modelId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateMachineModel(
            Guid modelId,
            [FromBody] AdminMachineModelRequest request)
        {
            try
            {
                var result = await _machineService.UpdateMachineModelAsync(modelId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("models/{modelId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteMachineModel(Guid modelId)
        {
            try
            {
                var result = await _machineService.DeleteMachineModelAsync(modelId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        

        
        [HttpPost("models/{modelId:guid}/sections")]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateMachineSection(
            Guid modelId,
            [FromBody] AdminMachineSectionRequest request)
        {
            try
            {
                var result = await _machineService.CreateMachineSectionAsync(modelId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPut("sections/{sectionId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateMachineSection(
            Guid sectionId,
            [FromBody] AdminMachineSectionRequest request)
        {
            try
            {
                var result = await _machineService.UpdateMachineSectionAsync(sectionId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("sections/{sectionId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteMachineSection(Guid sectionId)
        {
            try
            {
                var result = await _machineService.DeleteMachineSectionAsync(sectionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        

        

        
        [HttpPost("sections/{sectionId:guid}/parts")]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateMachinePart(
            Guid sectionId,
            [FromBody] AdminMachinePartRequest request)
        {
            try
            {
                var result = await _machineService.CreateMachinePartAsync(sectionId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPut("parts/{partId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateMachinePart(
            Guid partId,
            [FromBody] AdminMachinePartRequest request)
        {
            try
            {
                var result = await _machineService.UpdateMachinePartAsync(partId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("parts/{partId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteMachinePart(Guid partId)
        {
            try
            {
                var result = await _machineService.DeleteMachinePartAsync(partId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        // ---------- Hiyerarşi listeleme (silinmiş öğeler dahil) ----------

        [HttpGet("{machineId:guid}/models")]
        public async Task<ActionResult<List<AdminHierarchyItemDto>>> GetMachineModels(Guid machineId)
            => Ok(await _machineService.GetMachineModelsAsync(machineId));

        [HttpGet("models/{modelId:guid}/sections")]
        public async Task<ActionResult<List<AdminHierarchyItemDto>>> GetModelSections(Guid modelId)
            => Ok(await _machineService.GetModelSectionsAsync(modelId));

        [HttpGet("sections/{sectionId:guid}/parts")]
        public async Task<ActionResult<List<AdminHierarchyItemDto>>> GetSectionParts(Guid sectionId)
            => Ok(await _machineService.GetSectionPartsAsync(sectionId));

        // ---------- Silinen öğeyi geri getirme ----------

        [HttpPost("models/{modelId:guid}/restore")]
        public async Task<ActionResult<ApiResponse<string>>> RestoreMachineModel(Guid modelId)
            => Ok(await _machineService.RestoreMachineModelAsync(modelId));

        [HttpPost("sections/{sectionId:guid}/restore")]
        public async Task<ActionResult<ApiResponse<string>>> RestoreMachineSection(Guid sectionId)
            => Ok(await _machineService.RestoreMachineSectionAsync(sectionId));

        [HttpPost("parts/{partId:guid}/restore")]
        public async Task<ActionResult<ApiResponse<string>>> RestoreMachinePart(Guid partId)
            => Ok(await _machineService.RestoreMachinePartAsync(partId));
    }
}
