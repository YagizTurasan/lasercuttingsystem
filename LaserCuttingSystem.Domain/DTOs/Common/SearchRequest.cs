using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Common
{
    public record SearchRequest
    {
        public string? Query { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortBy { get; init; }
        public bool SortDescending { get; init; } = false;
        public Dictionary<string, object> Filters { get; init; } = new();
    }
}
