using AutoMapper;
using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Error;
using LaserCuttingSystem.Domain.Entities.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    public class ErrorCodeService : IErrorCodeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISubscriptionService _subscriptionService;

        public ErrorCodeService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ISubscriptionService subscriptionService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _subscriptionService = subscriptionService;
        }

        public async Task<List<ErrorCodeListDto>> GetAccessibleErrorCodesAsync(Guid? userId = null)
        {
            List<ErrorCode> errorCodes;

            if (!userId.HasValue)
            {
                // Kullanıcı giriş yapmamış - sadece ücretsiz hata kodları
                errorCodes = (await _unitOfWork.ErrorCodes.GetFreeAccessErrorCodesAsync()).ToList();
            }
            else
            {
                // Kullanıcıya göre erişilebilir hata kodlarını getir
                errorCodes = await GetUserAccessibleErrorCodes(userId.Value);
            }

            var result = new List<ErrorCodeListDto>();

            foreach (var errorCode in errorCodes)
            {
                var dto = _mapper.Map<ErrorCodeListDto>(errorCode);

                // Access bilgilerini zenginleştir
                dto = await EnrichWithAccessInfo(dto, errorCode, userId);

                result.Add(dto);
            }

            return result.OrderBy(e => e.Code).ToList();
        }

        public async Task<ErrorCodeDto?> GetErrorCodeByIdAsync(Guid errorCodeId, Guid? userId = null)
        {
            var errorCode = await _unitOfWork.ErrorCodes.GetByIdAsync(errorCodeId,
                e => e.Machine,
                e => e.MachinePart.MachineSection.MachineModel.Machine,
                e => e.ErrorSource);

            if (errorCode == null || !errorCode.IsActive)
            {
                return null;
            }

            // Erişim kontrolü
            var hasAccess = await CheckUserAccessToErrorCode(errorCode, userId);
            if (!hasAccess)
            {
                return null;
            }

            var dto = _mapper.Map<ErrorCodeDto>(errorCode);

            // Access bilgilerini zenginleştir
            dto = dto with
            {
                HasAccess = true,
                AccessLevel = errorCode.AccessLevel
            };

            return dto;
        }

        public async Task<List<ErrorCodeListDto>> GetErrorCodesByMachineAsync(Guid machineId, Guid? userId = null)
        {
            var errorCodes = await _unitOfWork.ErrorCodes.GetErrorCodesByMachineAsync(machineId);

            var result = new List<ErrorCodeListDto>();

            foreach (var errorCode in errorCodes)
            {
                // Kullanıcı erişim kontrolü
                var hasAccess = await CheckUserAccessToErrorCode(errorCode, userId);
                if (!hasAccess)
                {
                    continue; // Bu hata kodunu gösterme
                }

                var dto = _mapper.Map<ErrorCodeListDto>(errorCode);
                dto = await EnrichWithAccessInfo(dto, errorCode, userId);

                result.Add(dto);
            }

            return result.OrderBy(e => e.Code).ToList();
        }

        public async Task<List<ErrorCodeListDto>> GetFreeErrorCodesAsync()
        {
            var freeErrorCodes = await _unitOfWork.ErrorCodes.GetFreeAccessErrorCodesAsync();

            var result = new List<ErrorCodeListDto>();

            foreach (var errorCode in freeErrorCodes)
            {
                var dto = _mapper.Map<ErrorCodeListDto>(errorCode);
                dto = dto with
                {
                    HasAccess = true,
                    AccessLevel = "Ücretsiz"
                };

                result.Add(dto);
            }

            return result.OrderBy(e => e.Code).ToList();
        }

        public async Task<PagedResult<ErrorCodeListDto>> SearchErrorCodesAsync(SearchRequest request, Guid? userId = null)
        {
            List<ErrorCode> allErrorCodes;

            if (!userId.HasValue)
            {
                // Kullanıcı giriş yapmamış - sadece ücretsiz hata kodları
                allErrorCodes = (await _unitOfWork.ErrorCodes.GetFreeAccessErrorCodesAsync()).ToList();
            }
            else
            {
                // Kullanıcıya göre erişilebilir hata kodlarını getir
                allErrorCodes = await GetUserAccessibleErrorCodes(userId.Value);
            }

            // Arama filtresi uygula
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                allErrorCodes = allErrorCodes.Where(e =>
                    e.Code.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                    e.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description != null && e.Description.Contains(request.Query, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Machine != null && e.Machine.Brand.Contains(request.Query, StringComparison.OrdinalIgnoreCase)) ||
                    (e.MachinePart != null && e.MachinePart.PartName.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // Filtreleri uygula
            if (request.Filters != null && request.Filters.Any())
            {
                // Access level filtresi
                if (request.Filters.TryGetValue("accessLevel", out var accessLevel))
                {
                    var level = accessLevel?.ToString()?.ToLowerInvariant();
                    if (level == "free" || level == "ücretsiz")
                    {
                        allErrorCodes = allErrorCodes.Where(e => e.IsFreeAccess).ToList();
                    }
                    else if (level == "premium")
                    {
                        allErrorCodes = allErrorCodes.Where(e => !e.IsFreeAccess).ToList();
                    }
                }

                // Machine filtresi
                if (request.Filters.TryGetValue("machineId", out var machineId))
                {
                    if (Guid.TryParse(machineId?.ToString(), out var parsedMachineId))
                    {
                        allErrorCodes = allErrorCodes.Where(e =>
                            e.MachineId == parsedMachineId ||
                            (e.MachinePart?.MachineSection?.MachineModel?.MachineId == parsedMachineId)
                        ).ToList();
                    }
                }
            }

            // Sıralama uygula
            allErrorCodes = request.SortBy?.ToLowerInvariant() switch
            {
                "code" => request.SortDescending
                    ? allErrorCodes.OrderByDescending(e => e.Code).ToList()
                    : allErrorCodes.OrderBy(e => e.Code).ToList(),
                "title" => request.SortDescending
                    ? allErrorCodes.OrderByDescending(e => e.Title).ToList()
                    : allErrorCodes.OrderBy(e => e.Title).ToList(),
                "createdat" => request.SortDescending
                    ? allErrorCodes.OrderByDescending(e => e.CreatedAt).ToList()
                    : allErrorCodes.OrderBy(e => e.CreatedAt).ToList(),
                _ => allErrorCodes.OrderBy(e => e.Code).ToList()
            };

            // Sayfalama hesapla
            var totalCount = allErrorCodes.Count;
            var pagedErrorCodes = allErrorCodes
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // DTO'ya dönüştür ve zenginleştir
            var mappedItems = new List<ErrorCodeListDto>();
            foreach (var errorCode in pagedErrorCodes)
            {
                var dto = _mapper.Map<ErrorCodeListDto>(errorCode);
                dto = await EnrichWithAccessInfo(dto, errorCode, userId);
                mappedItems.Add(dto);
            }

            return new PagedResult<ErrorCodeListDto>
            {
                Items = mappedItems,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                HasPreviousPage = request.PageNumber > 1,
                HasNextPage = request.PageNumber < (int)Math.Ceiling((double)totalCount / request.PageSize)
            };
        }

        public async Task<List<ErrorSourceDto>> GetErrorSourcesAsync()
        {
            var sources = await _unitOfWork.ErrorSources.GetActiveSourcesAsync();
            return _mapper.Map<List<ErrorSourceDto>>(sources);
        }

        // Private helper methods
        private async Task<List<ErrorCode>> GetUserAccessibleErrorCodes(Guid userId)
        {
            // Kullanıcının subscription bilgilerini al
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);

            if (subscription == null)
            {
                // Abonelik yoksa sadece ücretsiz hata kodları
                return (await _unitOfWork.ErrorCodes.GetFreeAccessErrorCodesAsync()).ToList();
            }

            var subscriptionLevel = subscription.Plan.Level;
            var isEnterprise = subscription.Plan.CanAccessAllMachines;

            // Seçili makineleri al
            var selectedMachineIds = new List<Guid>();
            if (!isEnterprise)
            {
                var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
                selectedMachineIds = selections.Select(s => s.MachineId).ToList();
            }

            // Repository'den erişilebilir hata kodlarını al
            var accessibleCodes = await _unitOfWork.ErrorCodes
                .GetAccessibleErrorCodesForUserAsync(userId, subscriptionLevel, selectedMachineIds, isEnterprise);

            return accessibleCodes.ToList();
        }

        private async Task<bool> CheckUserAccessToErrorCode(ErrorCode errorCode, Guid? userId)
        {
            if (!userId.HasValue)
            {
                // Kullanıcı giriş yapmamış - sadece ücretsiz hata kodları
                return errorCode.IsFreeAccess;
            }

            // Kullanıcının subscription bilgilerini al
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId.Value);

            if (subscription == null)
            {
                // Abonelik yoksa sadece ücretsiz hata kodları
                return errorCode.IsFreeAccess;
            }

            var isEnterprise = subscription.Plan.CanAccessAllMachines;

            if (isEnterprise)
            {
                // Enterprise: Her şeye erişim
                return true;
            }

            // Seçili makineleri al
            var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId.Value);
            var selectedMachineIds = selections.Select(s => s.MachineId).ToList();

            // ErrorCode entity'sindeki IsAccessibleToUser metodunu kullan
            return errorCode.IsAccessibleToUser(subscription.Plan.Level, selectedMachineIds, isEnterprise);
        }

        private async Task<ErrorCodeListDto> EnrichWithAccessInfo(
            ErrorCodeListDto dto,
            ErrorCode errorCode,
            Guid? userId)
        {
            var hasAccess = await CheckUserAccessToErrorCode(errorCode, userId);

            // Hierarchy bilgilerini zenginleştir
            var machineBrand = errorCode.Machine?.Brand ??
                              errorCode.MachinePart?.MachineSection?.MachineModel?.Machine?.Brand;

            var partName = errorCode.MachinePart?.PartName;
            var sourceName = errorCode.ErrorSource?.SourceName;

            return dto with
            {
                HasAccess = hasAccess,
                AccessLevel = errorCode.AccessLevel,
                MachineBrand = machineBrand,
                PartName = partName,
                SourceName = sourceName,
                FullHierarchyPath = errorCode.FullHierarchyPath
            };
        }
    }
}