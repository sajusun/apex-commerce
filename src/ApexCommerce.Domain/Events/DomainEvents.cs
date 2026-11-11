using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.Events;

public sealed record OrderPlacedDomainEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerEmail,
    decimal TotalAmount,
    string Currency,
    List<Guid> VariantIds
) : IDomainEvent;

public sealed record OrderCancelledDomainEvent(
    Guid OrderId,
    string OrderNumber,
    string CancellationReason,
    List<(Guid VariantId, int Quantity)> ItemsToRelease
) : IDomainEvent;

public sealed record StockReservedDomainEvent(
    Guid VariantId,
    int ReservedQuantity,
    int RemainingStock
) : IDomainEvent;

public sealed record PaymentCompletedDomainEvent(
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string TransactionId
) : IDomainEvent;

