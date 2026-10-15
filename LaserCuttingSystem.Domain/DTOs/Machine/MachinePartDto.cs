using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Machine
{
    public record MachinePartDto
    {
        public Guid Id { get; init; }
        public string PartName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string FullHierarchyName { get; init; } = string.Empty;
    }
}
