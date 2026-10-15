using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Auth
{
    public record ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }
}
