using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Machine
{
    public record LaserMachineListDto
    {
        public Guid Id { get; init; }
        public string Brand { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string DisplayName { get; init; } = string.Empty;

        public bool IsSelected { get; init; }
        public int? SelectionOrder { get; init; }
        public bool HasAccess { get; init; }
        public string AccessType { get; init; } = string.Empty;
    }
}
