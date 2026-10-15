using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Events
{
    public record MachineSelectedEvent(
    Guid UserId,
    Guid MachineId,
    string MachineName,
    int SelectionOrder
) : DomainEvent;
}
