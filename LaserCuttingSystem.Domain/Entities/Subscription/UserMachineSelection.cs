using LaserCuttingSystem.Domain.Entities.Identity;
using LaserCuttingSystem.Domain.Entities.Machine;

namespace LaserCuttingSystem.Domain.Entities.Subscription
{
    public class UserMachineSelection : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid MachineId { get; set; }

        public int SelectionOrder { get; set; } = 1;

        public DateTime SelectedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual LaserMachine Machine { get; set; } = null!;
    }
}
