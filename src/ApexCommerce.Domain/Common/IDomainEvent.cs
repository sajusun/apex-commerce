using MediatR;

namespace ApexCommerce.Domain.Common;

/// <summary>
/// Marker interface for Domain Events dispatched across MediatR pipeline.
/// In Laravel: Like an Event class instantiated with event(new OrderPlaced($order)).
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId => Guid.NewGuid();
    DateTime OccurredOnUtc => DateTime.UtcNow;
}

