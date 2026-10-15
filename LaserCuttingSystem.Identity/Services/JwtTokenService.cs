using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Identity.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LaserCuttingSystem.Data.IRepositories;
using LaserCuttingSystem.Domain.Entities;
using LaserCuttingSystem.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LaserCuttingSystem.Identity.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        // JWT Configuration
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly int _accessTokenExpiryMinutes;
        private readonly int _refreshTokenExpiryDays;
        private readonly string _algorithm;
        public JwtTokenService(
           IConfiguration configuration,
           IDateTimeProvider dateTimeProvider,
           ILogger<JwtTokenService> logger,
           UserManager<ApplicationUser> userManager,
           IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;

            // JWT ayarlarını configuration'dan al
            var jwtConfig = _configuration.GetSection("JwtSettings");
            _jwtKey = jwtConfig["Key"] ?? throw new InvalidOperationException("JWT Key is required");
            _jwtIssuer = jwtConfig["Issuer"] ?? "LaserCuttingSystem";
            _jwtAudience = jwtConfig["Audience"] ?? "LaserCuttingSystemClient";
            _accessTokenExpiryMinutes = int.TryParse(jwtConfig["AccessTokenExpiryMinutes"], out var accessExpiry) ? accessExpiry : 60;
            _refreshTokenExpiryDays = int.TryParse(jwtConfig["RefreshTokenExpiryDays"], out var refreshExpiry) ? refreshExpiry : 7;
            _algorithm = SecurityAlgorithms.HmacSha256;
        }

        public async Task<TokenResult> GenerateTokenAsync(ApplicationUser user)
        {
            var (accessToken, refreshToken) = await GenerateTokenPairAsync(user);

            return new TokenResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = _dateTimeProvider.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                TokenType = "Bearer"
            };
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RequireExpirationTime = true
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(_algorithm, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
        {
            try
            {
                var claims = await GenerateClaimsAsync(user);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = _dateTimeProvider.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
                    Issuer = _jwtIssuer,
                    Audience = _jwtAudience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey)),
                        _algorithm)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                _logger.LogDebug("Access token generated for user {UserId}", user.Id);
                return tokenString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user {UserId}", user.Id);
                throw;
            }
        }

        public async Task<string> GenerateRefreshTokenAsync()
        {
            try
            {
                var randomBytes = new byte[64];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomBytes);

                var refreshToken = Convert.ToBase64String(randomBytes);

                _logger.LogDebug("Refresh token generated");
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token");
                throw;
            }
        }

        public async Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(ApplicationUser user)
        {
            var accessToken = await GenerateAccessTokenAsync(user);
            var refreshToken = await GenerateRefreshTokenAsync();

            await StoreRefreshTokenAsync(user.Id, refreshToken);

            _logger.LogInformation("Token pair generated for user {UserId}", user.Id);
            return (accessToken, refreshToken);
        }

        public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RequireExpirationTime = true
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(_algorithm, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid JWT token algorithm");
                    return null;
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogDebug("Token expired");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId)
        {
            try
            {
                var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

                if (storedToken == null)
                {
                    _logger.LogWarning("Refresh token not found: {Token}", refreshToken[..8] + "...");
                    return false;
                }

                if (storedToken.UserId != userId)
                {
                    _logger.LogWarning("Refresh token user mismatch. Token UserId: {TokenUserId}, Expected: {ExpectedUserId}",
                        storedToken.UserId, userId);
                    return false;
                }

                if (storedToken.ExpiresAt <= _dateTimeProvider.UtcNow)
                {
                    _logger.LogDebug("Refresh token expired for user {UserId}", userId);
                    _unitOfWork.RefreshTokens.Remove(storedToken);
                    await _unitOfWork.SaveChangesAsync();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
                if (storedToken == null || storedToken.ExpiresAt <= _dateTimeProvider.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    return null;
                }

                var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("User not found or inactive for refresh token");
                    return null;
                }

                _unitOfWork.RefreshTokens.Remove(storedToken);
                await _unitOfWork.SaveChangesAsync();

                var newTokenPair = await GenerateTokenPairAsync(user);

                _logger.LogInformation("Token refreshed for user {UserId}", user.Id);
                return newTokenPair;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            try
            {
                var storedToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
                if (storedToken != null)
                {
                    _unitOfWork.RefreshTokens.Remove(storedToken);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Refresh token revoked for user {UserId}", storedToken.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                throw;
            }
        }

        public async Task RevokeAllUserRefreshTokensAsync(Guid userId)
        {
            try
            {
                var userTokens = await _unitOfWork.RefreshTokens.FindAsync(t => t.UserId == userId);

                if (userTokens.Any())
                {
                    _unitOfWork.RefreshTokens.RemoveRange(userTokens);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all refresh tokens for user {UserId}", userId);
                throw;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jsonToken?.Claims?.FirstOrDefault(c =>
                    c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub");

                return userIdClaim?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting user ID from token");
                return null;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);

                var expClaim = jsonToken?.Claims?.FirstOrDefault(c => c.Type == "exp");
                if (expClaim == null) return true;

                if (long.TryParse(expClaim.Value, out var expUnixTime))
                {
                    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnixTime).UtcDateTime;
                    return expDateTime <= _dateTimeProvider.UtcNow;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        private async Task<List<Claim>> GenerateClaimsAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? ""),
                new(ClaimTypes.Name, user.FullName),
                new("unique_name", user.UserName ?? ""),
                new("first_name", user.FirstName),
                new("last_name", user.LastName),
                new("email_verified", user.EmailConfirmed.ToString()),
                new("user_active", user.IsActive.ToString()),
                new("jti", Guid.NewGuid().ToString()),
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrEmpty(user.CompanyName))
            {
                claims.Add(new Claim("company", user.CompanyName));
            }

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var activeSubscription = await _unitOfWork.UserSubscriptions.GetActiveSubscriptionByUserAsync(user.Id);
            if (activeSubscription != null)
            {
                claims.Add(new Claim("subscription_level", activeSubscription.Plan.Level.ToString()));
                claims.Add(new Claim("subscription_plan", activeSubscription.Plan.Name));
                claims.Add(new Claim("is_enterprise", activeSubscription.Plan.CanAccessAllMachines.ToString()));
                claims.Add(new Claim("subscription_expires", new DateTimeOffset(activeSubscription.EndDate).ToUnixTimeSeconds().ToString()));
            }
            else
            {
                // Fallback to free plan
                claims.Add(new Claim("subscription_level", "0"));
                claims.Add(new Claim("subscription_plan", "Free"));
                claims.Add(new Claim("is_enterprise", "False"));
            }

            return claims;
        }

        private async Task StoreRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var tokenEntity = new UserRefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiresAt = _dateTimeProvider.UtcNow.AddDays(_refreshTokenExpiryDays),
                CreatedAt = _dateTimeProvider.UtcNow
            };

            await _unitOfWork.RefreshTokens.AddAsync(tokenEntity);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}