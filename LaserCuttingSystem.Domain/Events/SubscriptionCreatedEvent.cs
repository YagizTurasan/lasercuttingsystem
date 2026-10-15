using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Events
{
    public record SubscriptionCreatedEvent(
    Guid SubscriptionId,
    Guid UserId,
    Guid PlanId,
    string PlanName,
    DateTime StartDate,
    DateTime EndDate
) : DomainEvent;
}
