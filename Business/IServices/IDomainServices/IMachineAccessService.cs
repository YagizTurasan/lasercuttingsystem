using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices.IDomainServices
{
    public interface IMachineAccessService
    {
        Task<bool> CanUserAccessMachine(Guid userId, Guid machineId);
        Task<bool> CanUserAccessErrorCode(Guid userId, Guid errorCodeId);
        Task<List<Guid>> GetAccessibleMachineIds(Guid userId);
        Task<int> GetUserSubscriptionLevel(Guid userId);
        Task<bool> IsUserEnterprise(Guid userId);
    }
}
