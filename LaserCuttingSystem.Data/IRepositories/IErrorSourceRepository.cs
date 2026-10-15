using LaserCuttingSystem.Domain.Entities.Error;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Data.IRepositories
{
    public interface IErrorSourceRepository : IRepository<ErrorSource>
    {
        Task<IEnumerable<ErrorSource>> GetActiveSourcesAsync();
    }
}
