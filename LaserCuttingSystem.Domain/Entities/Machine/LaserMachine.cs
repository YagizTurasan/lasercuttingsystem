using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaserCuttingSystem.Domain.Entities.Error;
using LaserCuttingSystem.Domain.Entities.Subscription;

namespace LaserCuttingSystem.Domain.Entities.Machine
{
    public class LaserMachine : AuditableEntity
    {
        [Required]
        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public virtual ICollection<MachineModel> MachineModels { get; set; } = new List<MachineModel>();
        public virtual ICollection<ErrorCode> ErrorCodes { get; set; } = new List<ErrorCode>();
        public virtual ICollection<UserMachineSelection> UserMachineSelections { get; set; } = new List<UserMachineSelection>();

        public bool IsSelectedByUser(Guid userId)
        {
            return UserMachineSelections.Any(s => s.UserId == userId && s.IsActive);
        }

        [NotMapped]
        public string DisplayName => Brand;
    }
}
