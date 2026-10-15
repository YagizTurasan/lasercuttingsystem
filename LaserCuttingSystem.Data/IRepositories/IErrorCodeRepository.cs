using LaserCuttingSystem.Domain.Entities.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IErrorCodeRepository : IRepository<ErrorCode>
    {
        Task<IEnumerable<ErrorCode>> GetFreeAccessErrorCodesAsync();
        Task<IEnumerable<ErrorCode>> GetErrorCodesByMachineAsync(Guid machineId);
        Task<IEnumerable<ErrorCode>> GetAccessibleErrorCodesForUserAsync(Guid userId, int subscriptionLevel, List<Guid> selectedMachineIds, bool isEnterprise);
        Task<IEnumerable<ErrorCode>> SearchErrorCodesAsync(string searchTerm, Guid userId, int subscriptionLevel, List<Guid> selectedMachineIds, bool isEnterprise);
    }
}
