using LaserCuttingSystem.Business.IServices.IAdminServices;
using LaserCuttingSystem.Domain.DTOs.Admin.Users;
using LaserCuttingSystem.Domain.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUserController : ControllerBase
    {
        private readonly IAdminUserService _userService;

        public AdminUserController(IAdminUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminUserListDto>>>> GetUsers(
            [FromBody] SearchRequest request)
        {
            try
            {
                var users = await _userService.GetUsersAsync(request);
                return Ok(ApiResponse<PagedResult<AdminUserListDto>>.CreateSuccess(users));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<PagedResult<AdminUserListDto>>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> GetUserById(Guid userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<AdminUserDetailDto>.Failure("Kullanıcı bulunamadı."));
                }

                return Ok(ApiResponse<AdminUserDetailDto>.CreateSuccess(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<AdminUserDetailDto>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost]
        
        public async Task<ActionResult<ApiResponse<Guid>>> CreateUser([FromBody] AdminCreateUserRequest request)
        {
            try
            {
                var result = await _userService.CreateUserAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return CreatedAtAction(nameof(GetUserById), new { userId = result.Data }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<Guid>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPut("{userId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateUser(
            Guid userId,
            [FromBody] AdminUpdateUserRequest request)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(userId, request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("{userId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteUser(Guid userId)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost("{userId:guid}/activate")]
        public async Task<ActionResult<ApiResponse<string>>> ActivateUser(Guid userId)
        {
            try
            {
                var result = await _userService.ActivateUserAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost("{userId:guid}/deactivate")]
        public async Task<ActionResult<ApiResponse<string>>> DeactivateUser(Guid userId)
        {
            try
            {
                var result = await _userService.DeactivateUserAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost("{userId:guid}/roles/{role}")]
        
        public async Task<ActionResult<ApiResponse<string>>> AssignRole(Guid userId, string role)
        {
            try
            {
                var result = await _userService.AssignRoleAsync(userId, role);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpDelete("{userId:guid}/roles/{role}")]
        
        public async Task<ActionResult<ApiResponse<string>>> RemoveRole(Guid userId, string role)
        {
            try
            {
                var result = await _userService.RemoveRoleAsync(userId, role);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpPost("{userId:guid}/subscription")]
        public async Task<ActionResult<ApiResponse<string>>> ChangeUserSubscription(
            Guid userId,
            [FromBody] AdminUserSubscriptionRequest request)
        {
            try
            {
                var result = await _userService.ChangeUserSubscriptionAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        [HttpPost("{userId:guid}/reset-password")]
        
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
            Guid userId,
            [FromBody] AdminResetPasswordRequest request)
        {
            try
            {
                var result = await _userService.ResetPasswordAsync(userId, request.NewPassword);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }

        
        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetAvailableRoles()
        {
            try
            {
                var roles = await _userService.GetAvailableRolesAsync();
                return Ok(ApiResponse<List<string>>.CreateSuccess(roles));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<string>>.Failure($"Bir hata oluştu: {ex.Message}"));
            }
        }
    }
}
