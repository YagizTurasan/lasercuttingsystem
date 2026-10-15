using LaserCuttingSystem.Domain.Entities.Machine;
using LaserCuttingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities.Error
{
    public class ErrorCode : AuditableEntity
    {
        public Guid? MachineId { get; set; }
        public Guid? MachinePartId { get; set; }
        public Guid? ErrorSourceId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Solutions { get; set; }

        public bool IsFreeAccess { get; set; } = false;
        public bool IsCommonAcrossAllMachines { get; set; } = false;

        [ForeignKey("MachineId")]
        public virtual LaserMachine? Machine { get; set; }

        [ForeignKey("MachinePartId")]
        public virtual MachinePart? MachinePart { get; set; }

        [ForeignKey("ErrorSourceId")]
        public virtual ErrorSource? ErrorSource { get; set; }

        public bool IsAccessibleToUser(int userSubscriptionLevel, List<Guid> userSelectedMachineIds, bool isEnterprise)
        {
            if (isEnterprise) return true;

            if (IsFreeAccess) return true;

            var targetMachineId = GetTargetMachineId();
            return targetMachineId.HasValue && userSelectedMachineIds.Contains(targetMachineId.Value);
        }

        private Guid? GetTargetMachineId()
        {
            if (MachineId.HasValue) return MachineId.Value;

            return MachinePart?.MachineSection?.MachineModel?.MachineId;
        }

        [NotMapped]
        public string AccessLevel => IsFreeAccess ? "Ücretsiz" : "Premium";

        [NotMapped]
        public string FullHierarchyPath => MachinePart != null
            ? $"{MachinePart.MachineSection?.MachineModel?.Machine?.Brand} " +
              $"{MachinePart.MachineSection?.MachineModel?.ModelCode} > " +
              $"{MachinePart.MachineSection?.SectionName} > " +
              $"{MachinePart.PartName} > {Code}"
            : Machine != null
                ? $"{Machine.Brand} > {Code}"
                : $"Genel > {Code}";
    }
}
