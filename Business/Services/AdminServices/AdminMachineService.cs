using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Admin.Machines;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.Entities.Machine;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services.AdminServices
{
    public class AdminMachineService : IAdminMachineService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminMachineService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<AdminMachineListDto>> GetMachinesAsync(SearchRequest request)
        {
            var query = _unitOfWork.LaserMachines.GetQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(request.Query))
            {
                query = query.Where(m => m.Brand.Contains(request.Query)
                                        || (m.Description != null && m.Description.Contains(request.Query)));
            }

            // Apply filters
            foreach (var filter in request.Filters)
            {
                switch (filter.Key.ToLower())
                {
                    case "isactive":
                        if (bool.TryParse(filter.Value.ToString(), out bool isActive))
                        {
                            query = query.Where(m => m.IsActive == isActive);
                        }
                        break;
                }
            }

            // Include related data
            query = query
                .Include(m => m.MachineModels)
                .Include(m => m.ErrorCodes)
                .Include(m => m.UserMachineSelections);

            // Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "brand":
                        query = request.SortDescending ? query.OrderByDescending(m => m.Brand) : query.OrderBy(m => m.Brand);
                        break;
                    case "createdat":
                        query = request.SortDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt);
                        break;
                    default:
                        query = query.OrderBy(m => m.Brand);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(m => m.Brand);
            }

            var totalCount = await query.CountAsync();

            var machines = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => new AdminMachineListDto
                {
                    Id = m.Id,
                    Brand = m.Brand,
                    Description = m.Description,
                    ModelsCount = m.MachineModels.Count,
                    ErrorCodesCount = m.ErrorCodes.Count,
                    UsersCount = m.UserMachineSelections.Where(ums => ums.IsActive).Count(),
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminMachineListDto>
            {
                Items = machines,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<ApiResponse<Guid>> CreateMachineAsync(AdminMachineCreateRequest request)
        {
            try
            {
                // Check if machine with same brand already exists
                var existingMachine = await _unitOfWork.LaserMachines
                    .FirstOrDefaultAsync(m => m.Brand.ToLower() == request.Brand.ToLower());

                if (existingMachine != null)
                {
                    return ApiResponse<Guid>.Failure("Bu marka ile kayıtlı bir makine zaten var.");
                }

                var machine = new LaserMachine
                {
                    Brand = request.Brand,
                    Description = request.Description
                };

                await _unitOfWork.LaserMachines.AddAsync(machine);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(machine.Id, "Makine başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateMachineAsync(Guid machineId, AdminMachineUpdateRequest request)
        {
            try
            {
                var machine = await _unitOfWork.LaserMachines.GetByIdAsync(machineId);
                if (machine == null)
                {
                    return ApiResponse<string>.Failure("Makine bulunamadı.");
                }

                // Check if another machine with same brand exists
                var existingMachine = await _unitOfWork.LaserMachines
                    .FirstOrDefaultAsync(m => m.Id != machineId && m.Brand.ToLower() == request.Brand.ToLower());

                if (existingMachine != null)
                {
                    return ApiResponse<string>.Failure("Bu marka ile kayıtlı başka bir makine var.");
                }

                machine.Brand = request.Brand;
                machine.Description = request.Description;
                machine.IsActive = request.IsActive;
                machine.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.LaserMachines.Update(machine);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Makine başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteMachineAsync(Guid machineId)
        {
            try
            {
                var machine = await _unitOfWork.LaserMachines
                    .GetByIdAsync(machineId, m => m.MachineModels, m => m.ErrorCodes, m => m.UserMachineSelections);

                if (machine == null)
                {
                    return ApiResponse<string>.Failure("Makine bulunamadı.");
                }

                // Check if machine has active users
                if (machine.UserMachineSelections.Any(ums => ums.IsActive))
                {
                    return ApiResponse<string>.Failure("Bu makine aktif kullanıcılar tarafından seçildiği için silinemez.");
                }

                // Soft delete
                machine.IsActive = false;
                machine.UpdatedAt = DateTime.UtcNow;

                // Deactivate related models and error codes
                foreach (var model in machine.MachineModels)
                {
                    model.IsActive = false;
                    model.UpdatedAt = DateTime.UtcNow;
                }

                foreach (var errorCode in machine.ErrorCodes)
                {
                    errorCode.IsActive = false;
                    errorCode.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.LaserMachines.Update(machine);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Makine başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Guid>> CreateMachineModelAsync(Guid machineId, AdminMachineModelRequest request)
        {
            try
            {
                var machine = await _unitOfWork.LaserMachines.GetByIdAsync(machineId);
                if (machine == null)
                {
                    return ApiResponse<Guid>.Failure("Makine bulunamadı.");
                }

                // Check if model with same code already exists for this machine
                var existingModel = await _unitOfWork.MachineModels
                    .FirstOrDefaultAsync(m => m.MachineId == machineId && m.ModelCode.ToLower() == request.ModelCode.ToLower());

                if (existingModel != null)
                {
                    return ApiResponse<Guid>.Failure("Bu makine için aynı model kodu zaten var.");
                }

                var model = new MachineModel
                {
                    MachineId = machineId,
                    ModelCode = request.ModelCode,
                    Description = request.Description
                };

                await _unitOfWork.MachineModels.AddAsync(model);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(model.Id, "Makine modeli başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateMachineModelAsync(Guid modelId, AdminMachineModelRequest request)
        {
            try
            {
                var model = await _unitOfWork.MachineModels.GetByIdAsync(modelId, m => m.Machine);
                if (model == null)
                {
                    return ApiResponse<string>.Failure("Model bulunamadı.");
                }

                // Check if another model with same code exists for this machine
                var existingModel = await _unitOfWork.MachineModels
                    .FirstOrDefaultAsync(m => m.Id != modelId && m.MachineId == model.MachineId && m.ModelCode.ToLower() == request.ModelCode.ToLower());

                if (existingModel != null)
                {
                    return ApiResponse<string>.Failure("Bu makine için aynı model kodu başka bir modelde kullanılıyor.");
                }

                model.ModelCode = request.ModelCode;
                model.Description = request.Description;
                model.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.MachineModels.Update(model);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Model başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteMachineModelAsync(Guid modelId)
        {
            try
            {
                var model = await _unitOfWork.MachineModels
                    .GetByIdAsync(modelId, m => m.MachineSections);

                if (model == null)
                {
                    return ApiResponse<string>.Failure("Model bulunamadı.");
                }

                // Soft delete
                model.IsActive = false;
                model.UpdatedAt = DateTime.UtcNow;

                // Deactivate related sections
                foreach (var section in model.MachineSections)
                {
                    section.IsActive = false;
                    section.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.MachineModels.Update(model);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Model başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Guid>> CreateMachineSectionAsync(Guid modelId, AdminMachineSectionRequest request)
        {
            try
            {
                var model = await _unitOfWork.MachineModels.GetByIdAsync(modelId);
                if (model == null)
                {
                    return ApiResponse<Guid>.Failure("Model bulunamadı.");
                }

                // Check if section with same name already exists for this model
                var existingSection = await _unitOfWork.MachineSections
                    .FirstOrDefaultAsync(s => s.MachineModelId == modelId && s.SectionName.ToLower() == request.SectionName.ToLower());

                if (existingSection != null)
                {
                    return ApiResponse<Guid>.Failure("Bu model için aynı bölüm adı zaten var.");
                }

                var section = new MachineSection
                {
                    MachineModelId = modelId,
                    SectionName = request.SectionName,
                    Description = request.Description
                };

                await _unitOfWork.MachineSections.AddAsync(section);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(section.Id, "Makine bölümü başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateMachineSectionAsync(Guid sectionId, AdminMachineSectionRequest request)
        {
            try
            {
                var section = await _unitOfWork.MachineSections.GetByIdAsync(sectionId, s => s.MachineModel);
                if (section == null)
                {
                    return ApiResponse<string>.Failure("Bölüm bulunamadı.");
                }

                // Check if another section with same name exists for this model
                var existingSection = await _unitOfWork.MachineSections
                    .FirstOrDefaultAsync(s => s.Id != sectionId && s.MachineModelId == section.MachineModelId && s.SectionName.ToLower() == request.SectionName.ToLower());

                if (existingSection != null)
                {
                    return ApiResponse<string>.Failure("Bu model için aynı bölüm adı başka bir bölümde kullanılıyor.");
                }

                section.SectionName = request.SectionName;
                section.Description = request.Description;
                section.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.MachineSections.Update(section);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Bölüm başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteMachineSectionAsync(Guid sectionId)
        {
            try
            {
                var section = await _unitOfWork.MachineSections
                    .GetByIdAsync(sectionId, s => s.MachineParts);

                if (section == null)
                {
                    return ApiResponse<string>.Failure("Bölüm bulunamadı.");
                }

                // Soft delete
                section.IsActive = false;
                section.UpdatedAt = DateTime.UtcNow;

                // Deactivate related parts
                foreach (var part in section.MachineParts)
                {
                    part.IsActive = false;
                    part.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.MachineSections.Update(section);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Bölüm başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<Guid>> CreateMachinePartAsync(Guid sectionId, AdminMachinePartRequest request)
        {
            try
            {
                var section = await _unitOfWork.MachineSections.GetByIdAsync(sectionId);
                if (section == null)
                {
                    return ApiResponse<Guid>.Failure("Bölüm bulunamadı.");
                }

                // Check if part with same name already exists for this section
                var existingPart = await _unitOfWork.MachineParts
                    .FirstOrDefaultAsync(p => p.MachineSectionId == sectionId && p.PartName.ToLower() == request.PartName.ToLower());

                if (existingPart != null)
                {
                    return ApiResponse<Guid>.Failure("Bu bölüm için aynı parça adı zaten var.");
                }

                var part = new MachinePart
                {
                    MachineSectionId = sectionId,
                    PartName = request.PartName,
                    Description = request.Description
                };

                await _unitOfWork.MachineParts.AddAsync(part);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(part.Id, "Makine parçası başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateMachinePartAsync(Guid partId, AdminMachinePartRequest request)
        {
            try
            {
                var part = await _unitOfWork.MachineParts.GetByIdAsync(partId, p => p.MachineSection);
                if (part == null)
                {
                    return ApiResponse<string>.Failure("Parça bulunamadı.");
                }

                // Check if another part with same name exists for this section
                var existingPart = await _unitOfWork.MachineParts
                    .FirstOrDefaultAsync(p => p.Id != partId && p.MachineSectionId == part.MachineSectionId && p.PartName.ToLower() == request.PartName.ToLower());

                if (existingPart != null)
                {
                    return ApiResponse<string>.Failure("Bu bölüm için aynı parça adı başka bir parçada kullanılıyor.");
                }

                part.PartName = request.PartName;
                part.Description = request.Description;
                part.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.MachineParts.Update(part);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Parça başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteMachinePartAsync(Guid partId)
        {
            try
            {
                var part = await _unitOfWork.MachineParts
                    .GetByIdAsync(partId, p => p.ErrorCodes);

                if (part == null)
                {
                    return ApiResponse<string>.Failure("Parça bulunamadı.");
                }

                // Soft delete
                part.IsActive = false;
                part.UpdatedAt = DateTime.UtcNow;

                // Deactivate related error codes
                foreach (var errorCode in part.ErrorCodes)
                {
                    errorCode.IsActive = false;
                    errorCode.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.MachineParts.Update(part);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Parça başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        // ---------- Hiyerarşi: listeleme (silinmişler dahil) ve geri getirme ----------

        public async Task<List<AdminHierarchyItemDto>> GetMachineModelsAsync(Guid machineId)
        {
            var models = await _unitOfWork.MachineModels.FindAsync(m => m.MachineId == machineId);
            return models
                .OrderByDescending(m => m.IsActive).ThenBy(m => m.ModelCode)
                .Select(m => new AdminHierarchyItemDto { Id = m.Id, Name = m.ModelCode, Description = m.Description, IsActive = m.IsActive })
                .ToList();
        }

        public async Task<List<AdminHierarchyItemDto>> GetModelSectionsAsync(Guid modelId)
        {
            var sections = await _unitOfWork.MachineSections.FindAsync(s => s.MachineModelId == modelId);
            return sections
                .OrderByDescending(s => s.IsActive).ThenBy(s => s.SectionName)
                .Select(s => new AdminHierarchyItemDto { Id = s.Id, Name = s.SectionName, Description = s.Description, IsActive = s.IsActive })
                .ToList();
        }

        public async Task<List<AdminHierarchyItemDto>> GetSectionPartsAsync(Guid sectionId)
        {
            var parts = await _unitOfWork.MachineParts.FindAsync(p => p.MachineSectionId == sectionId);
            return parts
                .OrderByDescending(p => p.IsActive).ThenBy(p => p.PartName)
                .Select(p => new AdminHierarchyItemDto { Id = p.Id, Name = p.PartName, Description = p.Description, IsActive = p.IsActive })
                .ToList();
        }

        // Geri getirme, silmenin pasifleştirdiği alt öğeleri de geri açar (model → bölümler, bölüm → parçalar, parça → hata kodları)
        public async Task<ApiResponse<string>> RestoreMachineModelAsync(Guid modelId)
        {
            try
            {
                var model = await _unitOfWork.MachineModels.GetByIdAsync(modelId, m => m.MachineSections);
                if (model == null) return ApiResponse<string>.Failure("Model bulunamadı.");

                model.IsActive = true;
                model.UpdatedAt = DateTime.UtcNow;
                foreach (var section in model.MachineSections)
                {
                    section.IsActive = true;
                    section.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.MachineModels.Update(model);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse<string>.CreateSuccess("", "Model geri getirildi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> RestoreMachineSectionAsync(Guid sectionId)
        {
            try
            {
                var section = await _unitOfWork.MachineSections.GetByIdAsync(sectionId, s => s.MachineParts);
                if (section == null) return ApiResponse<string>.Failure("Bölüm bulunamadı.");

                section.IsActive = true;
                section.UpdatedAt = DateTime.UtcNow;
                foreach (var part in section.MachineParts)
                {
                    part.IsActive = true;
                    part.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.MachineSections.Update(section);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse<string>.CreateSuccess("", "Bölüm geri getirildi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> RestoreMachinePartAsync(Guid partId)
        {
            try
            {
                var part = await _unitOfWork.MachineParts.GetByIdAsync(partId, p => p.ErrorCodes);
                if (part == null) return ApiResponse<string>.Failure("Parça bulunamadı.");

                part.IsActive = true;
                part.UpdatedAt = DateTime.UtcNow;
                foreach (var errorCode in part.ErrorCodes)
                {
                    errorCode.IsActive = true;
                    errorCode.UpdatedAt = DateTime.UtcNow;
                }

                _unitOfWork.MachineParts.Update(part);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse<string>.CreateSuccess("", "Parça geri getirildi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }
    }
}
