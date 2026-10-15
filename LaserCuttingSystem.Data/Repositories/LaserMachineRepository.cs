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
    public class LaserMachineRepository : BaseRepository<LaserMachine>, ILaserMachineRepository
    {
        public LaserMachineRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<LaserMachine>> GetMachinesWithErrorCodesAsync()
        {
            return await _dbSet
            .Include(m => m.ErrorCodes.Where(e => e.IsActive))
                .Where(m => m.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<LaserMachine>> GetSelectedMachinesByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(m => m.UserMachineSelections)
                .Where(m => m.IsActive && m.UserMachineSelections.Any(s => s.UserId == userId && s.IsActive))
                .OrderBy(m => m.UserMachineSelections.First(s => s.UserId == userId).SelectionOrder)
                .ToListAsync();
        }

        public async Task<LaserMachine?> GetMachineWithHierarchyAsync(Guid machineId)
        {
            return await _dbSet
                .Include(m => m.MachineModels)
                    .ThenInclude(model => model.MachineSections)
                        .ThenInclude(section => section.MachineParts)
                            .ThenInclude(part => part.ErrorCodes.Where(e => e.IsActive))
                .FirstOrDefaultAsync(m => m.Id == machineId && m.IsActive);
        }
    }
}
