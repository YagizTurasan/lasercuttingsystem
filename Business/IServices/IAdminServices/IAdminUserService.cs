using LaserCuttingSystem.Domain.DTOs.Admin.Users;
using LaserCuttingSystem.Domain.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Business.IServices.IAdminServices
{
    public interface IAdminUserService
    {
        Task<PagedResult<AdminUserListDto>> GetUsersAsync(SearchRequest request);
        Task<AdminUserDetailDto?> GetUserByIdAsync(Guid userId);
        Task<ApiResponse<Guid>> CreateUserAsync(AdminCreateUserRequest request);
        Task<ApiResponse<string>> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request);
        Task<ApiResponse<string>> DeleteUserAsync(Guid userId);
        Task<ApiResponse<string>> ActivateUserAsync(Guid userId);
        Task<ApiResponse<string>> DeactivateUserAsync(Guid userId);
        Task<ApiResponse<string>> AssignRoleAsync(Guid userId, string role);
        Task<ApiResponse<string>> RemoveRoleAsync(Guid userId, string role);
        Task<ApiResponse<string>> ChangeUserSubscriptionAsync(Guid userId, AdminUserSubscriptionRequest request);
        Task<ApiResponse<string>> ResetPasswordAsync(Guid userId, string newPassword);
        Task<List<string>> GetAvailableRolesAsync();
    }
}
