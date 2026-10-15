using LaserCuttingSystem.Core.Interfaces;
using LaserCuttingSystem.Core.Services;
using LaserCuttingSystem.Data.Context;
using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Identity;
using LaserCuttingSystem.Identity.Interfaces;
using LaserCuttingSystem.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace LaserCuttingSystem.API.Extensions
{
    public static class IdentityServiceExtensions
    {
        private const string PasswordResetTokenProviderName = "PasswordReset";

        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Core Services
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IAuthService, AuthService>();

            // JWT Services
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            // Configure Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 0;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                // Sign in settings
                // E-posta gönderimi (SMTP) yapılandırılmadan açılırsa yeni kullanıcılar giriş yapamaz
                options.SignIn.RequireConfirmedEmail = configuration.GetValue("Identity:RequireConfirmedEmail", false);
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.SignIn.RequireConfirmedAccount = false;

                // Şifre sıfırlama, doğrulamadan daha kısa ömürlü ayrı bir sağlayıcı kullanır
                options.Tokens.PasswordResetTokenProvider = PasswordResetTokenProviderName;
            })
            .AddEntityFrameworkStores<LaserCuttingDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<PasswordResetTokenProvider<ApplicationUser>>(PasswordResetTokenProviderName);

            // E-posta doğrulama bağlantısı 24 saat, şifre sıfırlama bağlantısı 1 saat geçerlidir
            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));
            services.Configure<PasswordResetTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(1));

            // Configure JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = jwtSettings.GetValue("RequireHttpsMetadata", true);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RequireExpirationTime = true
                };

                // JWT Events
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            var result = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                success = false,
                                message = "Unauthorized - Token required",
                                timestamp = DateTime.UtcNow
                            });
                            return context.Response.WriteAsync(result);
                        }
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = "Forbidden - Insufficient permissions",
                            timestamp = DateTime.UtcNow
                        });
                        return context.Response.WriteAsync(result);
                    }
                };
            });

            // Configure Authorization
            services.AddAuthorization(options =>
            {
                // Default policy - requires authenticated user
                //options.FallbackPolicy = options.DefaultPolicy;

                // Admin only policy
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));

                // Subscription-based policies for business logic
                options.AddPolicy("RequiresFreePlan", policy =>
                    policy.RequireAssertion(context =>
                        context.User.Identity.IsAuthenticated &&
                        (context.User.HasClaim("subscription_level", "0") ||
                         context.User.HasClaim("subscription_level", "1") ||
                         context.User.HasClaim("subscription_level", "2") ||
                         context.User.HasClaim("subscription_level", "3"))));

                options.AddPolicy("RequiresBasicPlan", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("subscription_level", "1") ||
                        context.User.HasClaim("subscription_level", "2") ||
                        context.User.HasClaim("subscription_level", "3")));

                options.AddPolicy("RequiresProfessionalPlan", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("subscription_level", "2") ||
                        context.User.HasClaim("subscription_level", "3")));

                options.AddPolicy("RequiresEnterprisePlan", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("subscription_level", "3") &&
                        context.User.HasClaim("is_enterprise", "True")));

                // Email verification policy (optional)
                options.AddPolicy("RequiresEmailConfirmed", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim("email_verified", "True")));
            });

            return services;
        }
    }
}