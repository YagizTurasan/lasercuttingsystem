using LaserCuttingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IUserRefreshTokenRepository : IRepository<UserRefreshToken>
    {
        
        Task<UserRefreshToken?> GetByTokenAsync(string token);
       
    }
}
