using LaserCuttingSystem.Domain.Entities.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IMachineModelRepository : IRepository<MachineModel>
    {
        Task<IEnumerable<MachineModel>> GetModelsByMachineAsync(Guid machineId);
    }
}
