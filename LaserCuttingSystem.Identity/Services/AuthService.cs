using LaserCuttingSystem.Business.IServices;
using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.DTOs.Auth;
using LaserCuttingSystem.Domain.DTOs.Common;
using LaserCuttingSystem.Domain.DTOs.Response;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Enums;
using LaserCuttingSystem.Identity.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtTokenService jwtTokenService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeProvider dateTimeProvider,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email ve şifre gereklidir");

            var user = await _userManager.Users
                .Include(u => u.UserSubscriptions.Where(s => s.IsActive && s.Status == SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Geçersiz email veya şifre");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Hesabınız devre dışı bırakılmış");

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    throw new UnauthorizedAccessException("Hesabınız geçici olarak kilitlenmiştir");
                if (result.IsNotAllowed)
                    throw new UnauthorizedAccessException("Giriş yapmanıza izin verilmiyor");
                throw new UnauthorizedAccessException("Geçersiz email veya şifre");
            }

            if (!user.EmailConfirmed && _userManager.Options.SignIn.RequireConfirmedEmail)
                throw new InvalidOperationException("Lütfen email adresinizi doğrulayın");

            // Role bilgisi UserManager üzerinden alınıyor
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var tokenResult = await _jwtTokenService.GenerateTokenAsync(user);

            user.UpdatedAt = _dateTimeProvider.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Successful login for user {UserId}", user.Id);

            return new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                TokenType = tokenResult.TokenType,
                User = new UserInfoResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    CompanyName = user.CompanyName,
                    EmailConfirmed = user.EmailConfirmed,
                    IsActive = user.IsActive,
                    Role = role
                }
            };
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            // Validation
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                throw new ArgumentException("Tüm zorunlu alanlar doldurulmalıdır");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email already exists {Email}", request.Email);
                throw new InvalidOperationException("Bu email adresi zaten kullanılıyor");
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                CompanyName = request.CompanyName?.Trim(),
                IsActive = true,
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Registration failed for email {Email}: {Errors}", request.Email, string.Join(", ", errors));
                throw new InvalidOperationException($"Kayıt işlemi başarısız: {string.Join(", ", errors)}");
            }

            // Add to default role
            await _userManager.AddToRoleAsync(user, "User");

            // Create free subscription
            try
            {
                await CreateFreeSubscriptionAsync(user.Id);
                _logger.LogInformation("Free subscription created for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create free subscription for user {UserId}", user.Id);
                // Don't throw - user creation should still succeed
            }

            // Send email verification if required
            var response = new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailConfirmed = user.EmailConfirmed,
                RequiresEmailConfirmation = _userManager.Options.SignIn.RequireConfirmedEmail
            };

            if (_userManager.Options.SignIn.RequireConfirmedEmail)
            {
                await SendEmailVerificationInternalAsync(user);
                response.Message = "Kayıt başarılı. Lütfen email adresinizi doğrulayın.";
            }
            else
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
                response.Message = "Kayıt başarılı. Hoş geldiniz!";
            }

            _logger.LogInformation("Successful registration for user {UserId}", user.Id);
            return response;
        }

        public async Task<TokenResult> RefreshTokenAsync(RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                throw new ArgumentException("Refresh token gereklidir");
            }

            var tokenPair = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken);
            if (tokenPair == null)
            {
                _logger.LogWarning("Invalid refresh token used");
                throw new UnauthorizedAccessException("Geçersiz refresh token");
            }

            return new TokenResult
            {
                AccessToken = tokenPair.Value.AccessToken,
                RefreshToken = tokenPair.Value.RefreshToken,
                ExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(60), // Should come from config
                TokenType = "Bearer"
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException("Refresh token gereklidir");
            }

            await _jwtTokenService.RevokeRefreshTokenAsync(refreshToken);
            _logger.LogInformation("User logged out successfully");
        }

        public async Task SendPasswordResetAsync(ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email adresi gereklidir");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                // Security: Don't reveal if email exists - just log and continue
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
                return;
            }

            // Identity token'ı tek kullanımlıktır (şifre değişince security stamp değişir) ve süresi sınırlıdır
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var emailSent = await _emailService.SendPasswordResetAsync(user.Email!, resetToken);

            if (!emailSent)
            {
                _logger.LogError("Failed to send password reset email to {Email}", user.Email);
                throw new InvalidOperationException("Email gönderimi sırasında hata oluştu");
            }

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new ArgumentException("Tüm alanlar gereklidir");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Password reset failed: User not found for email {Email}", request.Email);
                throw new InvalidOperationException("Geçersiz işlem");
            }

            // E-postadaki token Identity tarafından doğrulanır
            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                {
                    _logger.LogWarning("Invalid password reset token for email {Email}", request.Email);
                    throw new InvalidOperationException("Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş");
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Password reset failed for user {UserId}: {Errors}", user.Id, string.Join(", ", errors));
                throw new InvalidOperationException($"Şifre sıfırlama başarısız: {string.Join(", ", errors)}");
            }

            // Revoke all refresh tokens for security
            await _jwtTokenService.RevokeAllUserRefreshTokensAsync(user.Id);

            _logger.LogInformation("Password reset successful for user {UserId}", user.Id);
        }

        public async Task SendEmailVerificationAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                throw new InvalidOperationException("Kullanıcı bulunamadı");
            }

            if (user.EmailConfirmed)
            {
                throw new InvalidOperationException("Email adresi zaten doğrulanmış");
            }

            await SendEmailVerificationInternalAsync(user);
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("Email ve token gereklidir");
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                throw new InvalidOperationException("Geçersiz işlem");
            }

            if (user.EmailConfirmed)
            {
                throw new InvalidOperationException("Email adresi zaten doğrulanmış");
            }

            // Token doğrulanırsa Identity EmailConfirmed = true yapar
            user.UpdatedAt = _dateTimeProvider.UtcNow;
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                {
                    throw new InvalidOperationException("Doğrulama bağlantısı geçersiz veya süresi dolmuş");
                }
                throw new InvalidOperationException("Email doğrulama sırasında hata oluştu");
            }

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

            _logger.LogInformation("Email verified for user {UserId}", user.Id);
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new ArgumentException("Mevcut şifre ve yeni şifre gereklidir");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
            {
                throw new InvalidOperationException("Kullanıcı bulunamadı");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, string.Join(", ", errors));
                throw new InvalidOperationException($"Şifre değişikliği başarısız: {string.Join(", ", errors)}");
            }

            // Revoke all refresh tokens for security
            await _jwtTokenService.RevokeAllUserRefreshTokensAsync(userId);

            _logger.LogInformation("Password changed for user {UserId}", userId);
        }

        public async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            await _jwtTokenService.RevokeAllUserRefreshTokensAsync(userId);
            _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
        }

        // Private helper methods
        private async Task SendEmailVerificationInternalAsync(ApplicationUser user)
        {
            var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendEmailVerificationAsync(user.Email!, verificationToken);

            _logger.LogInformation("Email verification sent to user {UserId}", user.Id);
        }

        private async Task CreateFreeSubscriptionAsync(Guid userId)
        {
            try
            {
                var freePlan = await _unitOfWork.SubscriptionPlans.GetFreePlanAsync();
                if (freePlan == null)
                {
                    _logger.LogWarning("Free plan not found for new user {UserId}", userId);
                    return;
                }

                var subscription = new Domain.Entities.Subscription.UserSubscription
                {
                    UserId = userId,
                    PlanId = freePlan.Id,
                    StartDate = _dateTimeProvider.UtcNow,
                    EndDate = _dateTimeProvider.UtcNow.AddYears(1), // Free plan never expires
                    AutoRenew = false,
                    Status = Domain.Enums.SubscriptionStatus.Active
                };

                await _unitOfWork.UserSubscriptions.AddAsync(subscription);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Free subscription created for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create free subscription for user {UserId}", userId);
                // Don't throw - user creation should still succeed
            }
        }
    }
}