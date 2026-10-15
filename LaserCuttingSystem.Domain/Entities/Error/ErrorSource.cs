using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities.Error
{
    public class ErrorSource : AuditableEntity
    {
        [Required]
        [MaxLength(100)]
        public string SourceName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public virtual ICollection<ErrorCode> ErrorCodes { get; set; } = new List<ErrorCode>();
    }
}
