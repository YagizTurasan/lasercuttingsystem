using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Domain.DTOs.Subscription
{
    public record SubscribeRequest
    {
        [Required]
        public Guid PlanId { get; init; }

        public bool AutoRenew { get; init; } = true;
    }
}
