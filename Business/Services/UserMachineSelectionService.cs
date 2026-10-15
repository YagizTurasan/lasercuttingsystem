using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.Entities.Subscription;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services
{
    public class UserMachineSelectionService : IUserMachineSelectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserMachineSelectionService> _logger;

        public UserMachineSelectionService(IUnitOfWork unitOfWork, ILogger<UserMachineSelectionService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<ApiResponse<string>> DeselectMachineAsync(Guid userId, Guid machineId)
        {
            var selection = await _unitOfWork.UserMachineSelections
            .GetUserMachineSelectionAsync(userId, machineId);

            if (selection == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Seçim bulunamadı",
                    Errors = new List<string> { "Bu makine zaten seçili değil." }
                };
            }

            selection.IsActive = false;
            selection.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.UserMachineSelections.Update(selection);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deselected machine {MachineId}", userId, machineId);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Makine seçimi kaldırıldı"
            };
        }

        public async Task<ApiResponse<string>> SelectMachineAsync(Guid userId, Guid machineId)
        {
            var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
            if (subscription == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Aktif aboneliğiniz bulunmuyor",
                    Errors = new List<string> { "Makine seçimi yapmak için aktif bir aboneliğe sahip olmanız gerekiyor." }
                };
            }

            // Limitler kontrolü
            if (!subscription.Plan.CanAccessAllMachines && subscription.Plan.MaxMachineSelection != -1)
            {
                var currentCount = await _unitOfWork.UserMachineSelections.GetUserSelectionCountAsync(userId);
                if (currentCount >= subscription.Plan.MaxMachineSelection)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Makine seçim limitiniz doldu",
                        Errors = new List<string> { $"Planınız maksimum {subscription.Plan.MaxMachineSelection} makine seçimine izin veriyor." }
                    };
                }
            }

            // Zaten seçilmiş mi kontrol et
            var alreadySelected = await _unitOfWork.UserMachineSelections.IsUserSelectedMachineAsync(userId, machineId);
            if (alreadySelected)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "Bu makine zaten seçili",
                    Errors = new List<string> { "Bu makineyi zaten seçmişsiniz." }
                };
            }

            // Yeni seçim ekle
            var currentSelections = await _unitOfWork.UserMachineSelections.GetUserSelectionCountAsync(userId);
            var selection = new UserMachineSelection
            {
                UserId = userId,
                MachineId = machineId,
                SelectionOrder = currentSelections + 1,
                SelectedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserMachineSelections.AddAsync(selection);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User {UserId} selected machine {MachineId}", userId, machineId);

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Makine başarıyla seçildi",
                Data = selection.Id.ToString()
            };
        }
    }
}
