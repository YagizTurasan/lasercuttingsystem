using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.Repositories
{
    public class UserRefreshTokenRepository : BaseRepository<UserRefreshToken>,IUserRefreshTokenRepository
    {

        public UserRefreshTokenRepository(LaserCuttingDbContext context) : base(context)
        {
        }

        public async Task<UserRefreshToken?> GetByTokenAsync(string token)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Token == token);
        }
    }
}
