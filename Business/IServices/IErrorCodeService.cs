using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    public interface IErrorCodeService
    {
        Task<List<ErrorCodeListDto>> GetAccessibleErrorCodesAsync(Guid? userId = null);
        Task<ErrorCodeDto?> GetErrorCodeByIdAsync(Guid errorCodeId, Guid? userId = null);
        Task<List<ErrorCodeListDto>> GetErrorCodesByMachineAsync(Guid machineId, Guid? userId = null);
        Task<List<ErrorCodeListDto>> GetFreeErrorCodesAsync();
        Task<PagedResult<ErrorCodeListDto>> SearchErrorCodesAsync(SearchRequest request, Guid? userId = null);
        Task<List<ErrorSourceDto>> GetErrorSourcesAsync();

        // Analytics
        //Task LogErrorCodeViewAsync(Guid errorCodeId, Guid? userId, string? ipAddress = null);
    }
}
