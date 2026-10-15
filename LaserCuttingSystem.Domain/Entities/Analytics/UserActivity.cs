using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities.Analytics
{
    public class UserActivity : BaseEntity
    {
        public Guid? UserId { get; set; }

        [MaxLength(50)]
        public string ActivityType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }

}
