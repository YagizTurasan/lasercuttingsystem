using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Machine
{
    public record MachineSectionDto
    {
        public Guid Id { get; init; }
        public string SectionName { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
