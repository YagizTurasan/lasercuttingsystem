using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Error
{
    public record ErrorSourceDto
    {
        public Guid Id { get; init; }
        public string SourceName { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
