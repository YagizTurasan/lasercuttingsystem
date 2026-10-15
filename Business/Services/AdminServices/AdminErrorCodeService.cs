using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Admin.ErrorCodes;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Error;
using LaserCuttingSystem.Domain.Entities.Error;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services.AdminServices
{
    public class AdminErrorCodeService : IAdminErrorCodeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminErrorCodeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<AdminErrorCodeListDto>> GetErrorCodesAsync(SearchRequest request)
        {
            var query = _unitOfWork.ErrorCodes.GetQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(request.Query))
            {
                query = query.Where(ec => ec.Code.Contains(request.Query)
                                         || ec.Title.Contains(request.Query)
                                         || (ec.Description != null && ec.Description.Contains(request.Query)));
            }

            // Apply filters
            foreach (var filter in request.Filters)
            {
                switch (filter.Key.ToLower())
                {
                    case "isactive":
                        if (bool.TryParse(filter.Value.ToString(), out bool isActive))
                        {
                            query = query.Where(ec => ec.IsActive == isActive);
                        }
                        break;
                    case "isfreeaccess":
                        if (bool.TryParse(filter.Value.ToString(), out bool isFreeAccess))
                        {
                            query = query.Where(ec => ec.IsFreeAccess == isFreeAccess);
                        }
                        break;
                    case "machineid":
                        if (Guid.TryParse(filter.Value.ToString(), out Guid machineId))
                        {
                            query = query.Where(ec => ec.MachineId == machineId);
                        }
                        break;
                }
            }

            // Include related data
            query = query
                .Include(ec => ec.Machine)
                .Include(ec => ec.MachinePart)
                    .ThenInclude(mp => mp!.MachineSection)
                        .ThenInclude(ms => ms.MachineModel)
                            .ThenInclude(mm => mm.Machine)
                .Include(ec => ec.ErrorSource);

            // Sorting
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "code":
                        query = request.SortDescending ? query.OrderByDescending(ec => ec.Code) : query.OrderBy(ec => ec.Code);
                        break;
                    case "title":
                        query = request.SortDescending ? query.OrderByDescending(ec => ec.Title) : query.OrderBy(ec => ec.Title);
                        break;
                    case "createdat":
                        query = request.SortDescending ? query.OrderByDescending(ec => ec.CreatedAt) : query.OrderBy(ec => ec.CreatedAt);
                        break;
                    default:
                        query = query.OrderBy(ec => ec.Code);
                        break;
                }
            }
            else
            {
                query = query.OrderBy(ec => ec.Code);
            }

            var totalCount = await query.CountAsync();

            var errorCodes = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ec => new AdminErrorCodeListDto
                {
                    Id = ec.Id,
                    Code = ec.Code,
                    Title = ec.Title,
                    MachineBrand = ec.Machine != null ? ec.Machine.Brand :
                                  ec.MachinePart != null ? ec.MachinePart.MachineSection.MachineModel.Machine.Brand : null,
                    PartName = ec.MachinePart != null ? ec.MachinePart.PartName : null,
                    SourceName = ec.ErrorSource != null ? ec.ErrorSource.SourceName : null,
                    AccessLevel = ec.AccessLevel,
                    IsActive = ec.IsActive,
                    CreatedAt = ec.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AdminErrorCodeListDto>
            {
                Items = errorCodes,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<ApiResponse<Guid>> CreateErrorCodeAsync(AdminErrorCodeCreateRequest request)
        {
            try
            {
                // Check if error code already exists
                var existingErrorCode = await _unitOfWork.ErrorCodes
                    .FirstOrDefaultAsync(ec => ec.Code.ToLower() == request.Code.ToLower());

                if (existingErrorCode != null)
                {
                    return ApiResponse<Guid>.Failure("Bu kod ile kayıtlı bir hata kodu zaten var.");
                }

                // Validate machine/part relationships
                if (request.MachineId.HasValue)
                {
                    var machine = await _unitOfWork.LaserMachines.GetByIdAsync(request.MachineId.Value);
                    if (machine == null)
                    {
                        return ApiResponse<Guid>.Failure("Seçilen makine bulunamadı.");
                    }
                }

                if (request.MachinePartId.HasValue)
                {
                    var part = await _unitOfWork.MachineParts.GetByIdAsync(request.MachinePartId.Value);
                    if (part == null)
                    {
                        return ApiResponse<Guid>.Failure("Seçilen parça bulunamadı.");
                    }
                }

                if (request.ErrorSourceId.HasValue)
                {
                    var source = await _unitOfWork.ErrorSources.GetByIdAsync(request.ErrorSourceId.Value);
                    if (source == null)
                    {
                        return ApiResponse<Guid>.Failure("Seçilen hata kaynağı bulunamadı.");
                    }
                }

                var errorCode = new ErrorCode
                {
                    Code = request.Code,
                    Title = request.Title,
                    Description = request.Description,
                    Solutions = request.Solutions,
                    MachineId = request.MachineId,
                    MachinePartId = request.MachinePartId,
                    ErrorSourceId = request.ErrorSourceId,
                    IsFreeAccess = request.IsFreeAccess,
                    IsCommonAcrossAllMachines = request.IsCommonAcrossAllMachines
                };

                await _unitOfWork.ErrorCodes.AddAsync(errorCode);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(errorCode.Id, "Hata kodu başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateErrorCodeAsync(Guid errorCodeId, AdminErrorCodeUpdateRequest request)
        {
            try
            {
                var errorCode = await _unitOfWork.ErrorCodes.GetByIdAsync(errorCodeId);
                if (errorCode == null)
                {
                    return ApiResponse<string>.Failure("Hata kodu bulunamadı.");
                }

                // Check if another error code with same code exists
                var existingErrorCode = await _unitOfWork.ErrorCodes
                    .FirstOrDefaultAsync(ec => ec.Id != errorCodeId && ec.Code.ToLower() == request.Code.ToLower());

                if (existingErrorCode != null)
                {
                    return ApiResponse<string>.Failure("Bu kod başka bir hata kodunda kullanılıyor.");
                }

                // Validate relationships
                if (request.MachineId.HasValue)
                {
                    var machine = await _unitOfWork.LaserMachines.GetByIdAsync(request.MachineId.Value);
                    if (machine == null)
                    {
                        return ApiResponse<string>.Failure("Seçilen makine bulunamadı.");
                    }
                }

                if (request.MachinePartId.HasValue)
                {
                    var part = await _unitOfWork.MachineParts.GetByIdAsync(request.MachinePartId.Value);
                    if (part == null)
                    {
                        return ApiResponse<string>.Failure("Seçilen parça bulunamadı.");
                    }
                }

                if (request.ErrorSourceId.HasValue)
                {
                    var source = await _unitOfWork.ErrorSources.GetByIdAsync(request.ErrorSourceId.Value);
                    if (source == null)
                    {
                        return ApiResponse<string>.Failure("Seçilen hata kaynağı bulunamadı.");
                    }
                }

                errorCode.Code = request.Code;
                errorCode.Title = request.Title;
                errorCode.Description = request.Description;
                errorCode.Solutions = request.Solutions;
                errorCode.MachineId = request.MachineId;
                errorCode.MachinePartId = request.MachinePartId;
                errorCode.ErrorSourceId = request.ErrorSourceId;
                errorCode.IsFreeAccess = request.IsFreeAccess;
                errorCode.IsCommonAcrossAllMachines = request.IsCommonAcrossAllMachines;
                errorCode.IsActive = request.IsActive;
                errorCode.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ErrorCodes.Update(errorCode);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Hata kodu başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteErrorCodeAsync(Guid errorCodeId)
        {
            try
            {
                var errorCode = await _unitOfWork.ErrorCodes.GetByIdAsync(errorCodeId);
                if (errorCode == null)
                {
                    return ApiResponse<string>.Failure("Hata kodu bulunamadı.");
                }

                // Soft delete
                errorCode.IsActive = false;
                errorCode.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ErrorCodes.Update(errorCode);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Hata kodu başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<List<ErrorSourceDto>> GetErrorSourcesAsync()
        {
            var sources = await _unitOfWork.ErrorSources.GetAllAsync();

            return sources.Select(s => new ErrorSourceDto
            {
                Id = s.Id,
                SourceName = s.SourceName,
                Description = s.Description
            }).ToList();
        }

        public async Task<ApiResponse<Guid>> CreateErrorSourceAsync(AdminErrorSourceRequest request)
        {
            try
            {
                // Check if source with same name already exists
                var existingSource = await _unitOfWork.ErrorSources
                    .FirstOrDefaultAsync(s => s.SourceName.ToLower() == request.SourceName.ToLower());

                if (existingSource != null)
                {
                    return ApiResponse<Guid>.Failure("Bu isim ile kayıtlı bir hata kaynağı zaten var.");
                }

                var source = new ErrorSource
                {
                    SourceName = request.SourceName,
                    Description = request.Description
                };

                await _unitOfWork.ErrorSources.AddAsync(source);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<Guid>.CreateSuccess(source.Id, "Hata kaynağı başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> UpdateErrorSourceAsync(Guid sourceId, AdminErrorSourceRequest request)
        {
            try
            {
                var source = await _unitOfWork.ErrorSources.GetByIdAsync(sourceId);
                if (source == null)
                {
                    return ApiResponse<string>.Failure("Hata kaynağı bulunamadı.");
                }

                // Check if another source with same name exists
                var existingSource = await _unitOfWork.ErrorSources
                    .FirstOrDefaultAsync(s => s.Id != sourceId && s.SourceName.ToLower() == request.SourceName.ToLower());

                if (existingSource != null)
                {
                    return ApiResponse<string>.Failure("Bu isim başka bir hata kaynağında kullanılıyor.");
                }

                source.SourceName = request.SourceName;
                source.Description = request.Description;
                source.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ErrorSources.Update(source);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Hata kaynağı başarıyla güncellendi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> DeleteErrorSourceAsync(Guid sourceId)
        {
            try
            {
                var source = await _unitOfWork.ErrorSources
                    .GetByIdAsync(sourceId, s => s.ErrorCodes);

                if (source == null)
                {
                    return ApiResponse<string>.Failure("Hata kaynağı bulunamadı.");
                }

                // Check if source is used in any error codes
                if (source.ErrorCodes.Any(ec => ec.IsActive))
                {
                    return ApiResponse<string>.Failure("Bu hata kaynağı aktif hata kodlarında kullanıldığı için silinemez.");
                }

                // Soft delete
                source.IsActive = false;
                source.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.ErrorSources.Update(source);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<string>.CreateSuccess("", "Hata kaynağı başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}");
            }
        }
    }
}
