using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Domain.Entities;

public sealed class Payment : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string TransactionId { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? PayloadJson { get; private set; }

    private Payment() { } // EF Core

    public Payment(Guid id, Guid orderId, PaymentProvider provider, string transactionId, Money amount, PaymentStatus status, string? payloadJson = null)
        : base(id)
    {
        OrderId = orderId;
        Provider = provider;
        TransactionId = transactionId.Trim();
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Status = status;
        PayloadJson = payloadJson;
    }

    public void MarkCaptured(string transactionId)
    {
        TransactionId = transactionId.Trim();
        Status = PaymentStatus.Captured;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        PayloadJson = reason;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = PaymentStatus.Refunded;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

