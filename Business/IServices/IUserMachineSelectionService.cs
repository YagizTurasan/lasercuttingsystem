using LaserCuttingSystem.Domain.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    public interface IUserMachineSelectionService
    {
        Task<ApiResponse<string>> SelectMachineAsync(Guid userId, Guid machineId);
        Task<ApiResponse<string>> DeselectMachineAsync(Guid userId, Guid machineId);
    }
}
