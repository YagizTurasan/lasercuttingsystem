using LaserCuttingSystem.Domain.Entities.Machine;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IMachinePartRepository : IRepository<MachinePart>
    {
        Task<IEnumerable<MachinePart>> GetPartsBySectionAsync(Guid sectionId);
    }
}
