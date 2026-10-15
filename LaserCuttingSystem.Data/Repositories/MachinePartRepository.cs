using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Machine;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class MachinePartRepository : BaseRepository<MachinePart>, IMachinePartRepository
    {
        public MachinePartRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<MachinePart>> GetPartsBySectionAsync(Guid sectionId)
        {
            return await _dbSet
                .Include(p => p.MachineSection)
                .Where(p => p.MachineSectionId == sectionId && p.IsActive)
                .OrderBy(p => p.PartName) // SortOrder yerine PartName'e göre sırala
                .ToListAsync();
        }
    }
}
