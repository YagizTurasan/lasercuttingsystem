using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    public interface IMachineService
    {
        Task<List<LaserMachineListDto>> GetAllMachinesAsync(Guid? userId = null);
        Task<LaserMachineDto?> GetMachineByIdAsync(Guid machineId, Guid? userId = null);
        Task<List<MachineModelDto>> GetMachineModelsAsync(Guid machineId);
        Task<List<MachineSectionDto>> GetModelSectionsAsync(Guid modelId);
        Task<List<MachinePartDto>> GetSectionPartsAsync(Guid sectionId);
        Task<PagedResult<LaserMachineListDto>> SearchMachinesAsync(SearchRequest request, Guid? userId = null);

        // User Machine Selection
        Task<List<LaserMachineListDto>> GetUserSelectedMachinesAsync(Guid userId);
        Task<ApiResponse<string>> SelectMachineAsync(Guid userId, Guid machineId);
        Task<bool> UnselectMachineAsync(Guid userId, Guid machineId);
        Task<bool> ReorderMachineSelectionsAsync(Guid userId, List<Guid> machineIds);
    }
}
