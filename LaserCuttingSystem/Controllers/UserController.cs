using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaserCuttingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public UsersController(IUserService userService, ICurrentUserService currentUserService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var userId = _currentUserService.UserId!.Value;
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = _currentUserService.UserId!.Value;
            var result = await _userService.UpdateProfileAsync(userId, request);

            if (!result)
                return BadRequest(new { message = "Profil güncellemesi başarısız" });

            return Ok(new { message = "Profil başarıyla güncellendi" });
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PagedResult<UserListDto>>> GetUsers([FromQuery] SearchRequest request)
        {
            var result = await _userService.GetUsersAsync(request);
            return Ok(result);
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            var result = await _userService.DeactivateUserAsync(id);

            if (!result)
                return BadRequest(new { message = "Kullanıcı deaktivasyonu başarısız" });

            return Ok(new { message = "Kullanıcı deaktive edildi" });
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            var result = await _userService.ActivateUserAsync(id);

            if (!result)
                return BadRequest(new { message = "Kullanıcı aktivasyonu başarısız" });

            return Ok(new { message = "Kullanıcı aktive edildi" });
        }
    }
}
