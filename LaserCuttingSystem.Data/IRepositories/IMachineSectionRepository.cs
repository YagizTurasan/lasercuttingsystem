using LaserCuttingSystem.Domain.Entities.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IMachineSectionRepository : IRepository<MachineSection>
    {
        Task<IEnumerable<MachineSection>> GetSectionsByModelAsync(Guid modelId);
    }
}
