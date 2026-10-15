using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class UserMachineSelectionRepository : BaseRepository<UserMachineSelection>, IUserMachineSelectionRepository
    {
        public UserMachineSelectionRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<UserMachineSelection>> GetUserSelectionsAsync(Guid userId)
        {
            return await _dbSet
                .Include(s => s.Machine)
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderBy(s => s.SelectionOrder)
                .ToListAsync();
        }

        public async Task<int> GetUserSelectionCountAsync(Guid userId)
        {
            return await _dbSet
                .CountAsync(s => s.UserId == userId && s.IsActive);
        }

        public async Task<bool> IsUserSelectedMachineAsync(Guid userId, Guid machineId)
        {
            return await _dbSet
                .AnyAsync(s => s.UserId == userId && s.MachineId == machineId && s.IsActive);
        }

        public async Task<List<UserMachineSelection>> GetUserSelectionsOrderedAsync(Guid userId)
        {
            return await _dbSet
            .Include(s => s.Machine)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.SelectedAt) // En eski seçimler önce gelsin
            .ToListAsync();
        }

        public async Task DeactivateUserSelectionsAsync(List<Guid> selectionIds)
        {
            var selections = await _dbSet
            .Where(s => selectionIds.Contains(s.Id))
            .ToListAsync();

            foreach (var selection in selections)
            {
                selection.IsActive = false;
                selection.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeactivateExcessSelectionsAsync(Guid userId, int maxAllowed)
        {
            var allSelections = await GetUserSelectionsOrderedAsync(userId);

            if (allSelections.Count <= maxAllowed)
                return; // Limit aşmıyorsa işlem yapma

            // Fazla olan seçimleri belirle (eski seçimler önce kaldırılır)
            var excessSelections = allSelections.Skip(maxAllowed).ToList();
            var idsToDeactivate = excessSelections.Select(s => s.Id).ToList();

            await DeactivateUserSelectionsAsync(idsToDeactivate);
        }

        public async Task<UserMachineSelection?> GetUserMachineSelectionAsync(Guid userId, Guid machineId)
        {
            return await _dbSet
                 .FirstOrDefaultAsync(s => s.UserId == userId && s.MachineId == machineId && s.IsActive);
        }
    }

}
