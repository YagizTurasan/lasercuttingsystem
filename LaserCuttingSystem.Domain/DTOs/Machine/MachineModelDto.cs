using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Machine
{
    public record MachineModelDto
    {
        public Guid Id { get; init; }
        public string ModelCode { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string FullName { get; init; } = string.Empty;
    }
}
