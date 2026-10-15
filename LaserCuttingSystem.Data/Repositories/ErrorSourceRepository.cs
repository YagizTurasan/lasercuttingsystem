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
    public class ErrorSourceRepository : BaseRepository<ErrorSource>, IErrorSourceRepository
    {
        public ErrorSourceRepository(LaserCuttingDbContext context) : base(context) { }

        public async Task<IEnumerable<ErrorSource>> GetActiveSourcesAsync()
        {
            return await _dbSet
                .Where(s => s.IsActive)
                .OrderBy(s => s.SourceName) // SortOrder yerine SourceName'e göre sırala
                .ToListAsync();
        }
    }
}
