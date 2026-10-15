using LaserCuttingSystem.Domain.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IUserMachineSelectionRepository : IRepository<UserMachineSelection>
    {
        Task<IEnumerable<UserMachineSelection>> GetUserSelectionsAsync(Guid userId);
        Task<int> GetUserSelectionCountAsync(Guid userId);
        Task<bool> IsUserSelectedMachineAsync(Guid userId, Guid machineId);

        Task<List<UserMachineSelection>> GetUserSelectionsOrderedAsync(Guid userId);
        Task DeactivateUserSelectionsAsync(List<Guid> selectionIds);
        Task DeactivateExcessSelectionsAsync(Guid userId, int maxAllowed);
        Task<UserMachineSelection?> GetUserMachineSelectionAsync(Guid userId, Guid machineId);
    }
}
