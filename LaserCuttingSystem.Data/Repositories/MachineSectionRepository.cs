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
    public class MachineSectionRepository : BaseRepository<MachineSection>, IMachineSectionRepository
    {
        public MachineSectionRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<MachineSection>> GetSectionsByModelAsync(Guid modelId)
        {
            return await _dbSet
                .Include(s => s.MachineModel)
                .Where(s => s.MachineModelId == modelId && s.IsActive)
                .OrderBy(s => s.SectionName) // SortOrder yerine SectionName'e göre sırala
                .ToListAsync();
        }
    }
}
