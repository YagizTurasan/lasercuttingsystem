using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Error;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/error-codes")]
    [Authorize(Roles = "Admin")]
    public class AdminErrorCodeController : ControllerBase
    {
        private readonly IAdminErrorCodeService _errorCodeService;

        public AdminErrorCodeController(IAdminErrorCodeService errorCodeService)
        {
            _errorCodeService = errorCodeService;
        }

        
        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminErrorCodeListDto>>>> GetErrorCodes(
            [FromBody] SearchRequest request)
        {
            try
            {
                var errorCodes = await _errorCodeService.GetErrorCodesAsync(request);
                return Ok(ApiResponse<PagedResult<AdminErrorCodeListDto>>.CreateSuccess(errorCodes));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<AdminErrorCodeListDto>>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateErrorCode([FromBody] AdminErrorCodeCreateRequest request)
        {
            try
            {
                var result = await _errorCodeService.CreateErrorCodeAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetErrorCodes), result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPut("{errorCodeId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateErrorCode(
            Guid errorCodeId,
            [FromBody] AdminErrorCodeUpdateRequest request)
        {
            try
            {
                var result = await _errorCodeService.UpdateErrorCodeAsync(errorCodeId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

       
        [HttpDelete("{errorCodeId:guid}")]
        
        public async Task<ActionResult<ApiResponse<string>>> DeleteErrorCode(Guid errorCodeId)
        {
            try
            {
                var result = await _errorCodeService.DeleteErrorCodeAsync(errorCodeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        

        
        [HttpGet("sources")]
        public async Task<ActionResult<ApiResponse<List<ErrorSourceDto>>>> GetErrorSources()
        {
            try
            {
                var sources = await _errorCodeService.GetErrorSourcesAsync();
                return Ok(ApiResponse<List<ErrorSourceDto>>.CreateSuccess(sources));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<ErrorSourceDto>>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost("sources")]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateErrorSource([FromBody] AdminErrorSourceRequest request)
        {
            try
            {
                var result = await _errorCodeService.CreateErrorSourceAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPut("sources/{sourceId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateErrorSource(
            Guid sourceId,
            [FromBody] AdminErrorSourceRequest request)
        {
            try
            {
                var result = await _errorCodeService.UpdateErrorSourceAsync(sourceId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("sources/{sourceId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteErrorSource(Guid sourceId)
        {
            try
            {
                var result = await _errorCodeService.DeleteErrorSourceAsync(sourceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
    }
}

