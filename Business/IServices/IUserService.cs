using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices
{
    public interface IUserService
    {
        Task<UserDto?> GetUserByIdAsync(Guid userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<PagedResult<UserListDto>> GetUsersAsync(SearchRequest request);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<bool> DeactivateUserAsync(Guid userId);
        Task<bool> ActivateUserAsync(Guid userId);
        Task<List<string>> GetUserRolesAsync(Guid userId);
        Task<bool> AssignRoleAsync(Guid userId, string role);
        Task<bool> RemoveRoleAsync(Guid userId, string role);
    }
}
