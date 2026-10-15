using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Auth
{
    public record ResetPasswordRequest
    {
        [Required]
        public string Token { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; init; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
