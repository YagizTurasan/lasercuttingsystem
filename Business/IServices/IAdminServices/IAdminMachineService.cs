using LaserCuttingSystem.Domain.DTOs.Admin.Machines;
using LaserCuttingSystem.Domain.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices.IAdminServices
{
    public interface IAdminMachineService
    {
        Task<PagedResult<AdminMachineListDto>> GetMachinesAsync(SearchRequest request);
        Task<ApiResponse<Guid>> CreateMachineAsync(AdminMachineCreateRequest request);
        Task<ApiResponse<string>> UpdateMachineAsync(Guid machineId, AdminMachineUpdateRequest request);
        Task<ApiResponse<string>> DeleteMachineAsync(Guid machineId);

        // Machine Models
        Task<ApiResponse<Guid>> CreateMachineModelAsync(Guid machineId, AdminMachineModelRequest request);
        Task<ApiResponse<string>> UpdateMachineModelAsync(Guid modelId, AdminMachineModelRequest request);
        Task<ApiResponse<string>> DeleteMachineModelAsync(Guid modelId);

        // Machine Sections
        Task<ApiResponse<Guid>> CreateMachineSectionAsync(Guid modelId, AdminMachineSectionRequest request);
        Task<ApiResponse<string>> UpdateMachineSectionAsync(Guid sectionId, AdminMachineSectionRequest request);
        Task<ApiResponse<string>> DeleteMachineSectionAsync(Guid sectionId);

        // Machine Parts
        Task<ApiResponse<Guid>> CreateMachinePartAsync(Guid sectionId, AdminMachinePartRequest request);
        Task<ApiResponse<string>> UpdateMachinePartAsync(Guid partId, AdminMachinePartRequest request);
        Task<ApiResponse<string>> DeleteMachinePartAsync(Guid partId);

        // Hiyerarşi listeleme (silinmiş öğeler dahil) ve geri getirme
        Task<List<AdminHierarchyItemDto>> GetMachineModelsAsync(Guid machineId);
        Task<List<AdminHierarchyItemDto>> GetModelSectionsAsync(Guid modelId);
        Task<List<AdminHierarchyItemDto>> GetSectionPartsAsync(Guid sectionId);
        Task<ApiResponse<string>> RestoreMachineModelAsync(Guid modelId);
        Task<ApiResponse<string>> RestoreMachineSectionAsync(Guid sectionId);
        Task<ApiResponse<string>> RestoreMachinePartAsync(Guid partId);
    }
}
