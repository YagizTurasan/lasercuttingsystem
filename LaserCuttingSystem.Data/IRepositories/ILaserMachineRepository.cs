using LaserCuttingSystem.Domain.Entities.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface ILaserMachineRepository : IRepository<LaserMachine>
    {
        Task<IEnumerable<LaserMachine>> GetMachinesWithErrorCodesAsync();
        Task<IEnumerable<LaserMachine>> GetSelectedMachinesByUserAsync(Guid userId);
        Task<LaserMachine?> GetMachineWithHierarchyAsync(Guid machineId);
    }
}
