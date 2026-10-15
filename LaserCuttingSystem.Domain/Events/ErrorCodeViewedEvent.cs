using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Events
{
    public record ErrorCodeViewedEvent(
    Guid ErrorCodeId,
    Guid? UserId,
    string? UserSubscriptionLevel,
    string? IpAddress
) : DomainEvent;
}
