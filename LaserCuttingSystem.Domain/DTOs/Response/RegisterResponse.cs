using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Response
{
    public record RegisterResponse
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool EmailConfirmed { get; init; }
        public bool RequiresEmailConfirmation { get; init; }
        public string Message { get; set; } = string.Empty;
    }
}
