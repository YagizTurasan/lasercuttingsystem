using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Error
{
    public record ErrorCodeListDto
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Solutions { get; set; }

        public string? MachineBrand { get; init; }
        public string? PartName { get; init; }
        public string? SourceName { get; init; }
        public string FullHierarchyPath { get; init; } = string.Empty;

        public string AccessLevel { get; init; } = string.Empty;
        public bool HasAccess { get; init; }
    }
}
