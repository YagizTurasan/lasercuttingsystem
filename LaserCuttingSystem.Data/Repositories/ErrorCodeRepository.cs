using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities.Error;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class ErrorCodeRepository : BaseRepository<ErrorCode>, IErrorCodeRepository
    {
        public ErrorCodeRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<ErrorCode>> GetFreeAccessErrorCodesAsync()
        {
            return await _dbSet
                .Include(e => e.Machine)
                .Include(e => e.MachinePart)
                    .ThenInclude(p => p.MachineSection)
                        .ThenInclude(s => s.MachineModel)
                            .ThenInclude(m => m.Machine)
                .Include(e => e.ErrorSource)
                .Where(e => e.IsActive && e.IsFreeAccess)
                .ToListAsync();
        }

        public async Task<IEnumerable<ErrorCode>> GetErrorCodesByMachineAsync(Guid machineId)
        {
            return await _dbSet
                .Include(e => e.Machine)
                .Include(e => e.MachinePart)
                    .ThenInclude(p => p.MachineSection)
                        .ThenInclude(s => s.MachineModel)
                .Include(e => e.ErrorSource)
                .Where(e => e.IsActive &&
                           (e.MachineId == machineId ||
                            (e.MachinePart != null && e.MachinePart.MachineSection.MachineModel.MachineId == machineId)))
                .ToListAsync();
        }

        public async Task<IEnumerable<ErrorCode>> GetAccessibleErrorCodesForUserAsync(Guid userId, int subscriptionLevel, List<Guid> selectedMachineIds, bool isEnterprise)
        {
            var query = _dbSet
                .Include(e => e.Machine)
                .Include(e => e.MachinePart)
                    .ThenInclude(p => p.MachineSection)
                        .ThenInclude(s => s.MachineModel)
                            .ThenInclude(m => m.Machine)
                .Include(e => e.ErrorSource)
                .Where(e => e.IsActive);

            if (isEnterprise)
            {
                // Enterprise: Tüm hata kodları
                return await query.ToListAsync();
            }

            // Free access error codes + selected machine error codes
            query = query.Where(e =>
                e.IsFreeAccess ||
                (e.MachineId.HasValue && selectedMachineIds.Contains(e.MachineId.Value)) ||
                (e.MachinePart != null && e.MachinePart.MachineSection.MachineModel.MachineId != null &&
                 selectedMachineIds.Contains(e.MachinePart.MachineSection.MachineModel.MachineId))
            );

            return await query.ToListAsync();
        }
        public async Task<IEnumerable<ErrorCode>> SearchErrorCodesAsync(string searchTerm, Guid userId, int subscriptionLevel, List<Guid> selectedMachineIds, bool isEnterprise)
        {
            var accessibleCodes = await GetAccessibleErrorCodesForUserAsync(userId, subscriptionLevel, selectedMachineIds, isEnterprise);

            return accessibleCodes.Where(e =>
                e.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (e.Description != null && e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }
}
