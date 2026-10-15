using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities.Machine
{
    public class MachineModel : AuditableEntity
    {
        [Required]
        public Guid MachineId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ModelCode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey("MachineId")]
        public virtual LaserMachine Machine { get; set; } = null!;

        public virtual ICollection<MachineSection> MachineSections { get; set; } = new List<MachineSection>();

        [NotMapped]
        public string FullName => $"{Machine?.Brand} {ModelCode}";
    }
}
