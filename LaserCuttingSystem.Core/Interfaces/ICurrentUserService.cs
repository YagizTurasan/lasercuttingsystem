using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Core.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? UserEmail { get; }
        string? UserName { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
        string? GetClaim(string claimType);
        void PrintAllClaims();
    }
}
