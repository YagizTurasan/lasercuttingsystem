namespace LaserCuttingSystem.Domain.Events;

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid UserId,
    Guid SubscriptionId,
    decimal Amount,
    string Currency,
    string PaymentProvider
) : DomainEvent;