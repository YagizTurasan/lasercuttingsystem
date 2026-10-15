using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.Events
{
    public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName
) : DomainEvent;
}
