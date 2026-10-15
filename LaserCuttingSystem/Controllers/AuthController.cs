using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Domain.DTOs.Auth;
using LaserCuttingSystem.Domain.DTOs.Response;
using LaserCuttingSystem.Identity;
using LaserCuttingSystem.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LaserCuttingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Refresh token JavaScript'ten okunamasın diye yanıt gövdesi yerine httpOnly cookie ile taşınır
        private const string RefreshTokenCookieName = "lcs_refresh_token";
        private const string RefreshTokenCookiePath = "/api/Auth";

        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AuthController> _logger;
        private readonly int _refreshTokenExpiryDays;

        public AuthController(
            IAuthService authService,
            ICurrentUserService currentUser,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _authService = authService;
            _currentUser = currentUser;
            _logger = logger;
            _refreshTokenExpiryDays = configuration.GetValue("JwtSettings:RefreshTokenExpiryDays", 7);
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = RefreshTokenCookiePath,
                Expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpiryDays)
            });
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = RefreshTokenCookiePath
            });
        }

        // Tarayıcı istemcileri cookie kullanır; cookie yoksa (ör. Swagger, mobil) body'deki token kabul edilir
        private string? ResolveRefreshToken(RefreshTokenRequest? request)
        {
            if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
                return cookieToken;

            return string.IsNullOrWhiteSpace(request?.RefreshToken) ? null : request.RefreshToken;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz veri",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var response = await _authService.LoginAsync(request); // LoginAsync zaten role bilgisi ile döndürmeli
                _logger.LogInformation("Successful login for user: {Email}", request.Email);

                SetRefreshTokenCookie(response.RefreshToken);
                return Ok(response with { RefreshToken = string.Empty });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Login failed for {Email}: {Error}", request.Email, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
                return StatusCode(500, new { message = "Sunucu hatası oluştu" });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz veri",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var response = await _authService.RegisterAsync(request);

                _logger.LogInformation("Successful registration for user: {Email}", request.Email);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);
                return StatusCode(500, new { message = "Sunucu hatası oluştu" });
            }
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResult>> RefreshToken(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request)
        {
            try
            {
                var refreshToken = ResolveRefreshToken(request);
                if (refreshToken == null)
                {
                    return Unauthorized(new { message = "Refresh token gereklidir" });
                }

                var response = await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });

                SetRefreshTokenCookie(response.RefreshToken);
                response.RefreshToken = string.Empty;
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                DeleteRefreshTokenCookie();
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "Token yenileme hatası" });
            }
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request)
        {
            try
            {
                var refreshToken = ResolveRefreshToken(request);
                DeleteRefreshTokenCookie();

                if (refreshToken != null)
                {
                    await _authService.LogoutAsync(refreshToken);
                }
                return Ok(new { message = "Çıkış başarılı" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Çıkış sırasında hata oluştu" });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                await _authService.SendPasswordResetAsync(request);
                return Ok(new { message = "Şifre sıfırlama bağlantısı email adresinize gönderildi" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email");
                return StatusCode(500, new { message = "Email gönderimi sırasında hata oluştu" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz veri",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Şifre başarıyla sıfırlandı" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { message = "Şifre sıfırlama sırasında hata oluştu" });
            }
        }

        [HttpPost("send-email-verification")]
        public async Task<ActionResult> SendEmailVerification([FromBody] SendEmailVerificationRequest request)
        {
            try
            {
                if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email adresi gereklidir" });
                }

                await _authService.SendEmailVerificationAsync(request.Email);
                return Ok(new { message = "Doğrulama emaili gönderildi" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification");
                return StatusCode(500, new { message = "Email gönderimi sırasında hata oluştu" });
            }
        }

        public record SendEmailVerificationRequest
        {
            [Required]
            [EmailAddress]
            public string Email { get; init; } = string.Empty;
        }

        [HttpPost("verify-email")]
        public async Task<ActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Email ve token gereklidir" });
                }

                await _authService.VerifyEmailAsync(request);
                return Ok(new { message = "Email adresiniz başarıyla doğrulandı" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(500, new { message = "Email doğrulama sırasında hata oluştu" });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz veri",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var userId = _currentUser.UserId;
                if (userId == null)
                {
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });
                }

                await _authService.ChangePasswordAsync(userId.Value, request);
                return Ok(new { message = "Şifre başarıyla değiştirildi" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Şifre değiştirme sırasında hata oluştu" });
            }
        }

        [HttpPost("revoke-all-tokens")]
        [Authorize]
        public async Task<ActionResult> RevokeAllTokens()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                {
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });
                }

                await _authService.RevokeAllRefreshTokensAsync(userId.Value);
                DeleteRefreshTokenCookie();
                return Ok(new { message = "Tüm oturumlar sonlandırıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking tokens for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Oturum sonlandırma sırasında hata oluştu" });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult> GetProfile()
        {
            try
            {
                var userId = _currentUser.UserId;
                if (userId == null)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var firstName = _currentUser.GetClaim("first_name");
                var lastName = _currentUser.GetClaim("last_name");
                var email = _currentUser.GetClaim("email") ??
                _currentUser.GetClaim(ClaimTypes.Email) ??
                _currentUser.UserEmail;
                var company = _currentUser.GetClaim("company");

                return Ok(new
                {
                    id = userId.Value,
                    email = email,
                    firstName = firstName,
                    lastName = lastName,
                    fullName = $"{firstName} {lastName}".Trim(),
                    company = company,
                    isAuthenticated = _currentUser.IsAuthenticated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", _currentUser.UserId);
                return StatusCode(500, new { message = "Profil bilgileri alınamadı" });
            }
        }

    }
}
