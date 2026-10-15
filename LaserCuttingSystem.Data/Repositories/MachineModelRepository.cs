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
    public class MachineModelRepository : BaseRepository<MachineModel>, IMachineModelRepository
    {
        public MachineModelRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<MachineModel>> GetModelsByMachineAsync(Guid machineId)
        {
            return await _dbSet
                .Include(m => m.Machine)
                .Where(m => m.MachineId == machineId && m.IsActive)
                .OrderBy(m => m.ModelCode) // SortOrder yerine ModelCode'a göre sırala
                .ToListAsync();
        }
    }
}
