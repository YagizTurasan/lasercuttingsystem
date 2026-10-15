using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Auth
{
    public record RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; init; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string LastName { get; init; } = string.Empty;

        [MaxLength(200)]
        public string? CompanyName { get; init; }

        public bool AcceptTerms { get; init; } = false;
    }

}
