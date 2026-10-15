using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities.Machine
{
    public class MachineSection : AuditableEntity
    {
        [Required]
        public Guid MachineModelId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SectionName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey("MachineModelId")]
        public virtual MachineModel MachineModel { get; set; } = null!;

        public virtual ICollection<MachinePart> MachineParts { get; set; } = new List<MachinePart>();
    }
}
