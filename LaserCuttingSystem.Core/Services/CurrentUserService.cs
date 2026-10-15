using LaserCuttingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace LaserCuttingSystem.Core.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private string? _cachedUserId;

        public Guid? UserId
        {
            get
            {
                if (_cachedUserId == null)
                {
                    _cachedUserId = GetClaim(ClaimTypes.NameIdentifier);
                }
                return Guid.TryParse(_cachedUserId, out var userId) ? userId : null;
            }
        }

        public string? UserEmail => GetClaim(ClaimTypes.Email);

        public string? UserName => GetClaim("unique_name");

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }


        public string? GetClaim(string claimType)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // Null veya kimlik doğrulaması başarısızsa null döner
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
                return null;

            // Öncelikle doğrudan belirtilen claim aranır
            var claim = user.FindFirst(claimType);
            if (claim != null)
                return claim.Value;

            // Alternatif claim kontrolleri
            if (claimType == ClaimTypes.NameIdentifier)
            {
                claim = user.FindFirst("nameid") ?? user.FindFirst("sub");
                return claim?.Value;
            }

            if (claimType == ClaimTypes.Email)
            {
                claim = user.FindFirst("email") ?? user.FindFirst("unique_name");
                return claim?.Value;
            }

            return null;
        }

        // 🔍 DEBUG: Kullanıcının tüm claim’lerini konsola yazdır
        public void PrintAllClaims()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                Console.WriteLine("HttpContext.User null veya kimlik doğrulanmamış.");
                return;
            }

            Console.WriteLine(">>> Kullanıcı claim'leri:");
            foreach (var claim in user.Claims)
            {
                Console.WriteLine($" - {claim.Type}: {claim.Value}");
            }
        }
    }
}
