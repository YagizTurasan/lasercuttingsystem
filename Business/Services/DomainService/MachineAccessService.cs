using LaserCuttingSystem.Business.IServices.IDomainServices;
using LaserCuttingSystem.Data.IRepositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.Services.DomainService
{
    public class MachineAccessService : IMachineAccessService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MachineAccessService> _logger;

        public MachineAccessService(
            IUnitOfWork unitOfWork,
            ILogger<MachineAccessService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> CanUserAccessMachine(Guid userId, Guid machineId)
        {
            try
            {
                // Kullanıcının aktif aboneliğini al
                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                if (subscription == null) return false; // Abonelik yoksa erişim yok

                // ---------- Enterprise Plan ----------
                // Level 3 veya CanAccessAllMachines = true
                if (subscription.Plan.CanAccessAllMachines) return true; // Tüm makinelere erişim

                // ---------- Free Plan ----------
                // Level 0
                if (subscription.Plan.Level == 0) return false; // Hiç makineye erişim yok

                // ---------- Paid Plan ----------
                // Level 1 = 1 makine, Level 2 = 3 makine
                var userSelections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
                var selectedMachineIds = userSelections.Select(s => s.MachineId).ToList();

                // MaxMachineSelection kontrolü
                if (subscription.Plan.MaxMachineSelection > 0 && selectedMachineIds.Count > subscription.Plan.MaxMachineSelection)
                {
                    _logger.LogWarning("User {UserId} exceeds MaxMachineSelection limit", userId);
                    return false; // Seçim limiti aşıldı
                }

                // Sadece seçilen makineler erişilebilir
                return selectedMachineIds.Contains(machineId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking machine access for user {UserId}, machine {MachineId}", userId, machineId);
                return false;
            }
        }

        public async Task<bool> CanUserAccessErrorCode(Guid userId, Guid errorCodeId)
        {
            try
            {
                var errorCode = await _unitOfWork.ErrorCodes.GetByIdAsync(
                    errorCodeId,
                    e => e.Machine,
                    e => e.MachinePart.MachineSection.MachineModel.Machine);

                if (errorCode == null || !errorCode.IsActive) return false;

                if (errorCode.IsFreeAccess) return true;

                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                if (subscription == null) return false;

                if (subscription.Plan.CanAccessAllMachines) return true;

                var selectedMachineIds = (await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId))
                    .Select(s => s.MachineId)
                    .ToList();

                // Error code hangi makineye ait?
                var machineId = errorCode.MachineId ?? errorCode.MachinePart?.MachineSection?.MachineModel?.MachineId;
                if (machineId == null) return false;

                if (!selectedMachineIds.Contains(machineId.Value)) return false;

                // Entity üzerindeki iş kurallarını uygula
                return errorCode.IsAccessibleToUser(subscription.Plan.Level, selectedMachineIds, subscription.Plan.CanAccessAllMachines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking error code access for user {UserId}, error code {ErrorCodeId}", userId, errorCodeId);
                return false;
            }
        }

        public async Task<List<Guid>> GetAccessibleMachineIds(Guid userId)
        {
            try
            {
                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                if (subscription == null) return new List<Guid>();

                if (subscription.Plan.CanAccessAllMachines)
                {
                    var allMachines = await _unitOfWork.LaserMachines.FindAsync(m => m.IsActive);
                    return allMachines.Select(m => m.Id).ToList();
                }

                var selectedMachineIds = (await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId))
                    .Select(s => s.MachineId)
                    .ToList();

                // MaxMachineSelection kontrolü
                if (subscription.Plan.MaxMachineSelection > 0 && selectedMachineIds.Count > subscription.Plan.MaxMachineSelection)
                {
                    selectedMachineIds = selectedMachineIds.Take(subscription.Plan.MaxMachineSelection).ToList();
                }

                return selectedMachineIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accessible machine IDs for user {UserId}", userId);
                return new List<Guid>();
            }
        }

        public async Task<int> GetUserSubscriptionLevel(Guid userId)
        {
            try
            {
                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                return subscription?.Plan?.Level ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> IsUserEnterprise(Guid userId)
        {
            try
            {
                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                return subscription?.Plan?.CanAccessAllMachines ?? false;
            }
            catch
            {
                return false;
            }
        }

        // --- Helper method: Kullanıcıya özel DTO bilgisi ---
        public async Task<(bool isSelected, int? selectionOrder, bool hasAccess, string accessType)> GetUserMachineAccessInfo(Guid userId, Guid machineId)
        {
            var selections = await _unitOfWork.UserMachineSelections.GetUserSelectionsAsync(userId);
            var userSelection = selections.FirstOrDefault(s => s.MachineId == machineId);

            bool isSelected = userSelection != null && userSelection.IsActive;
            int? selectionOrder = isSelected ? userSelection.SelectionOrder : null;

            bool hasAccess = await CanUserAccessMachine(userId, machineId);
            string accessType = "Free";
            if (hasAccess)
            {
                var subscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(userId);
                if (subscription?.Plan?.CanAccessAllMachines == true)
                {
                    accessType = "Enterprise";
                }
                else if (isSelected)
                {
                    accessType = "Selected";
                }
            }

            return (isSelected, selectionOrder, hasAccess, accessType);
        }
    }
}
