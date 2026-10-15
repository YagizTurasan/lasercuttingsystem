using LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices.IAdminServices
{
    public interface IAdminErrorCodeService
    {
        Task<PagedResult<AdminErrorCodeListDto>> GetErrorCodesAsync(SearchRequest request);
        Task<ApiResponse<Guid>> CreateErrorCodeAsync(AdminErrorCodeCreateRequest request);
        Task<ApiResponse<string>> UpdateErrorCodeAsync(Guid errorCodeId, AdminErrorCodeUpdateRequest request);
        Task<ApiResponse<string>> DeleteErrorCodeAsync(Guid errorCodeId);

        // Error Sources
        Task<List<ErrorSourceDto>> GetErrorSourcesAsync();
        Task<ApiResponse<Guid>> CreateErrorSourceAsync(AdminErrorSourceRequest request);
        Task<ApiResponse<string>> UpdateErrorSourceAsync(Guid sourceId, AdminErrorSourceRequest request);
        Task<ApiResponse<string>> DeleteErrorSourceAsync(Guid sourceId);
    }
}
