using AutoMapper;
using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Machine;
using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    public class MachineService : IMachineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ISubscriptionService _subscriptionService;

        public MachineService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ISubscriptionService subscriptionService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _subscriptionService = subscriptionService;
        }

        public async Task<List<LaserMachineListDto>> GetAllMachinesAsync(Guid? userId = null)
        {
            var machines = await _unitOfWork.LaserMachines.GetAllAsync();
            var result = new List<LaserMachineListDto>();

            foreach (var machine in machines.Where(m => m.IsActive))
            {
                var dto = _mapper.Map<LaserMachineListDto>(machine);

                if (userId.HasValue)
                {
                    dto = await EnrichWithUserAccessInfo(dto, machine.Id, userId.Value);
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<LaserMachineDto?> GetMachineByIdAsync(Guid machineId, Guid? userId = null)
        {
            var machine = await _unitOfWork.LaserMachines.GetByIdAsync(machineId,
                m => m.MachineModels);

            if (machine == null || !machine.IsActive)
                return null;

            var dto = _mapper.Map<LaserMachineDto>(machine);

            if (userId.HasValue)
            {
                var enrichedDto = await EnrichWithUserAccessInfo(
                    new LaserMachineListDto
                    {
                        Id = dto.Id,
                        Brand = dto.Brand,
                        Description = dto.Description,
                        DisplayName = dto.DisplayName
                    },
                    machineId,
                    userId.Value);

                dto = dto with
                {
                    IsSelected = enrichedDto.IsSelected,
                    SelectionOrder = enrichedDto.SelectionOrder,
                    HasAccess = enrichedDto.HasAccess,
                    AccessType = enrichedDto.AccessType
                };
            }

            return dto;
        }

        public async Task<List<MachineModelDto>> GetMachineModelsAsync(Guid machineId)
        {
            var models = await _unitOfWork.MachineModels.GetModelsByMachineAsync(machineId);
            return _mapper.Map<List<MachineModelDto>>(models);
        }

        public async Task<List<MachineSectionDto>> GetModelSectionsAsync(Guid modelId)
        {
            var sections = await _unitOfWork.MachineSections.GetSectionsByModelAsync(modelId);
            return _mapper.Map<List<MachineSectionDto>>(sections);
        }

        public async Task<List<MachinePartDto>> GetSectionPartsAsync(Guid sectionId)
        {
            var parts = await _unitOfWork.MachineParts.GetPartsBySectionAsync(sectionId);
            return _mapper.Map<List<MachinePartDto>>(parts);
        }

        public async Task<PagedResult<LaserMachineListDto>> SearchMachinesAsync(SearchRequest request, Guid? userId = null)
        {
            var machines = await _unitOfWork.LaserMachines.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                machines = machines.Where(m =>
                    m.Brand.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                    (m.Description != null && m.Description.Contains(request.Query, StringComparison.OrdinalIgnoreCase)));
            }

            machines = machines.Where(m => m.IsActive);

            machines = request.SortBy?.ToLowerInvariant() switch
            {
                "brand" => request.SortDescending ? machines.OrderByDescending(m => m.Brand) : machines.OrderBy(m => m.Brand),
                "createdat" => request.SortDescending ? machines.OrderByDescending(m => m.CreatedAt) : machines.OrderBy(m => m.CreatedAt),
                _ => machines.OrderBy(m => m.Brand)
            };

            var totalCount = machines.Count();
            var items = machines
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var mappedItems = new List<LaserMachineListDto>();
            foreach (var machine in items)
            {
                var dto = _mapper.Map<LaserMachineListDto>(machine);

                if (userId.HasValue)
                    dto = await EnrichWithUserAccessInfo(dto, machine.Id, userId.Value);

                mappedItems.Add(dto);
            }

            return new PagedResult<LaserMachineListDto>
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

        // USER MACHINE SELECTION METHODS
        public async Task<List<LaserMachineListDto>> GetUserSelectedMachinesAsync(Guid userId)
        {
            var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
            var result = new List<LaserMachineListDto>();

            foreach (var selection in selections)
            {
                var dto = _mapper.Map<LaserMachineListDto>(selection.Machine);
                dto = dto with
                {
                    IsSelected = true,
                    SelectionOrder = selection.SelectionOrder,
                    HasAccess = true,
                    AccessType = "Selected"
                };

                result.Add(dto);
            }

            return result.OrderBy(m => m.SelectionOrder).ToList();
        }

        public async Task<ApiResponse<string>> SelectMachineAsync(Guid userId, Guid machineId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);

            if (subscription == null)
                return new ApiResponse<string> { Success = false, Message = "Aktif bir planınız yok", Errors = new List<string>() };

            // Limit kontrolü
            var canSelect = await _subscriptionService.CanUserSelectMoreMachinesAsync(userId);
            if (!canSelect)
            {
                var remaining = await _subscriptionService.GetRemainingMachineSelectionsAsync(userId);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Makine seçim limitine ulaştınız",
                    Errors = new List<string> { $"Kalan seçim hakkınız: {remaining}" }
                };
            }

            var machine = await _unitOfWork.LaserMachines.GetByIdAsync(machineId);
            if (machine == null || !machine.IsActive)
                return new ApiResponse<string> { Success = false, Message = "Makine bulunamadı" };

            var isAlreadySelected = await _unitOfWork.UserMachineSelections.IsUserSelectedMachineAsync(userId, machineId);
            if (isAlreadySelected)
                return new ApiResponse<string> { Success = false, Message = "Makine zaten seçili" };

            var currentSelections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
            var nextOrder = currentSelections.Count() + 1;

            var selection = new UserMachineSelection
            {
                UserId = userId,
                MachineId = machineId,
                SelectionOrder = nextOrder,
                SelectedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.UserMachineSelections.AddAsync(selection);
            await _unitOfWork.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Message = $"{machine.Brand} makinesi seçildi",
                Data = selection.Id.ToString()
            };
        }

        public async Task<bool> UnselectMachineAsync(Guid userId, Guid machineId)
        {
            var selections = await _unitOfWork.UserMachineSelections.FindAsync(
                s => s.UserId == userId && s.MachineId == machineId && s.IsActive);

            var selection = selections.FirstOrDefault();
            if (selection == null) return false;

            selection.IsActive = false;
            selection.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserMachineSelections.Update(selection);

            await ReorderUserSelections(userId);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderMachineSelectionsAsync(Guid userId, List<Guid> machineIds)
        {
            var currentSelections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);

            var currentMachineIds = currentSelections.Select(s => s.MachineId).ToList();
            if (!machineIds.All(id => currentMachineIds.Contains(id)) || machineIds.Count != currentMachineIds.Count)
                return false;

            for (int i = 0; i < machineIds.Count; i++)
            {
                var selection = currentSelections.First(s => s.MachineId == machineIds[i]);
                selection.SelectionOrder = i + 1;
                selection.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.UserMachineSelections.Update(selection);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // PRIVATE HELPERS
        private async Task<LaserMachineListDto> EnrichWithUserAccessInfo(LaserMachineListDto dto, Guid machineId, Guid userId)
        {
            var isSelected = await _unitOfWork.UserMachineSelections.IsUserSelectedMachineAsync(userId, machineId);
            var selectionOrder = 0;

            if (isSelected)
            {
                var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
                var userSelection = selections.FirstOrDefault(s => s.MachineId == machineId);
                selectionOrder = userSelection?.SelectionOrder ?? 0;
            }

            var hasAccess = await _subscriptionService.CanUserAccessMachineAsync(userId, machineId);

            // ---------- Access Type ----------
            var accessType = "Free"; // Default (Free plan veya erişim yok)
            if (hasAccess)
            {
                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);

                if (subscription?.Plan?.CanAccessAllMachines == true)
                    accessType = "Enterprise"; // Tüm makineler erişilebilir
                else if (isSelected)
                    accessType = "Selected"; // Seçili makine
            }

            return dto with
            {
                IsSelected = isSelected,
                SelectionOrder = selectionOrder > 0 ? selectionOrder : null,
                HasAccess = hasAccess,
                AccessType = accessType
            };
        }

        private async Task ReorderUserSelections(Guid userId)
        {
            var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);

            for (int i = 0; i < selections.Count(); i++)
            {
                var selection = selections.ToList()[i];
                selection.SelectionOrder = i + 1;
                selection.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.UserMachineSelections.Update(selection);
            }
        }
    }
}
