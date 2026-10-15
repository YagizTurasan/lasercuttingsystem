using System;

namespace LaserCuttingSystem.Domain.DTOs.Admin.Machines
{
    /// <summary>
    /// Admin panelindeki model / bölüm / parça ağacının bir düğümü. Silinmiş (pasif) öğeleri de içerir.
    /// </summary>
    public record AdminHierarchyItemDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; }
    }
}
