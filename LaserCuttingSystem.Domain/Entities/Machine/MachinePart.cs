using LaserCuttingSystem.Domain.Entities.Error;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities.Machine
{
    public class MachinePart : AuditableEntity
    {
        [Required]
        public Guid MachineSectionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PartName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey("MachineSectionId")]
        public virtual MachineSection MachineSection { get; set; } = null!;

        public virtual ICollection<ErrorCode> ErrorCodes { get; set; } = new List<ErrorCode>();

        [NotMapped]
        public string FullHierarchyName =>
            $"{MachineSection?.MachineModel?.Machine?.Brand} " +
            $"{MachineSection?.MachineModel?.ModelCode} - " +
            $"{MachineSection?.SectionName} - {PartName}";
    }
}
