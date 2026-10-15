using LaserCuttingSystem.Domain.DTOs.Auth;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Identity.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<TokenResult> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(string refreshToken);
        Task SendPasswordResetAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task SendEmailVerificationAsync(string email);
        Task VerifyEmailAsync(VerifyEmailRequest request);
        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task RevokeAllRefreshTokensAsync(Guid userId);
    }
}
