using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Entities
{
    public abstract class AuditableEntity : BaseEntity
    {
        public Guid? CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }
    }
}
