using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Events;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Domain.Entities;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderStatusHistory> _statusHistory = [];

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public Address? BillingAddress { get; private set; }
    public Money SubTotal { get; private set; } = null!;
    public Money DiscountAmount { get; private set; } = null!;
    public Money TaxAmount { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    public string? CouponCode { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? ShippingCarrier { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(
        Guid id,
        string orderNumber,
        Guid customerId,
        string customerEmail,
        Address shippingAddress,
        Address? billingAddress,
        Money shippingCost,
        string? couponCode = null,
        Money? discountAmount = null,
        Money? taxAmount = null)
    {
        var order = new Order
        {
            Id = id,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
            ShippingAddress = shippingAddress ?? throw new ArgumentNullException(nameof(shippingAddress)),
            BillingAddress = billingAddress ?? shippingAddress,
            ShippingCost = shippingCost ?? Money.Zero(),
            CouponCode = couponCode?.Trim().ToUpperInvariant(),
            DiscountAmount = discountAmount ?? Money.Zero(),
            TaxAmount = taxAmount ?? Money.Zero(),
            SubTotal = Money.Zero(),
            TotalAmount = Money.Zero(),
            CreatedAtUtc = DateTime.UtcNow
        };

        order._statusHistory.Add(new OrderStatusHistory(
            Guid.NewGuid(), order.Id, null, OrderStatus.PendingPayment, "Order placed and waiting for payment.", "System"));

        return order;
    }

    public void AddItem(Guid variantId, Guid productId, string productName, Sku sku, Money unitPrice, int quantity)
    {
        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException($"Cannot add items when order is in state '{Status}'.");

        var item = new OrderItem(Guid.NewGuid(), Id, variantId, productId, productName, sku, unitPrice, quantity);
        _items.Add(item);

        RecalculateTotals();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CompletePlacement()
    {
        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot complete placement for an order without items.");

        RaiseDomainEvent(new OrderPlacedDomainEvent(
            Id,
            OrderNumber,
            CustomerId,
            CustomerEmail,
            TotalAmount.Amount,
            TotalAmount.Currency,
            _items.Select(i => i.VariantId).ToList()
        ));
    }

    public void MarkPaymentReceived(string transactionId)
    {
        if (PaymentStatus == PaymentStatus.Captured) return;

        PaymentStatus = PaymentStatus.Captured;
        TransitionTo(OrderStatus.Processing, $"Payment captured via transaction {transactionId}.", "System");
    }

    public void MarkAsShipped(string trackingNumber, string carrier, string operatorName)
    {
        if (Status != OrderStatus.Processing)
            throw new InvalidOrderStateTransitionException(Status.ToString(), OrderStatus.Shipped.ToString());

        TrackingNumber = trackingNumber.Trim();
        ShippingCarrier = carrier.Trim();
        TransitionTo(OrderStatus.Shipped, $"Shipped via {carrier} (Tracking: {trackingNumber}).", operatorName);
    }

    public void MarkAsDelivered(string operatorName)
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOrderStateTransitionException(Status.ToString(), OrderStatus.Delivered.ToString());

        TransitionTo(OrderStatus.Delivered, "Shipment delivered to customer.", operatorName);
    }

    public void Cancel(string reason, string operatorName)
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOrderStateTransitionException(Status.ToString(), OrderStatus.Cancelled.ToString());

        if (Status == OrderStatus.Cancelled) return;

        CancellationReason = reason.Trim();
        TransitionTo(OrderStatus.Cancelled, $"Order cancelled: {reason}", operatorName);

        RaiseDomainEvent(new OrderCancelledDomainEvent(
            Id,
            OrderNumber,
            reason,
            _items.Select(i => (i.VariantId, i.Quantity)).ToList()
        ));
    }

    public void Refund(string reason, string operatorName)
    {
        if (PaymentStatus != PaymentStatus.Captured)
            throw new InvalidOperationException("Cannot refund an unpaid or already refunded order.");

        PaymentStatus = PaymentStatus.Refunded;
        TransitionTo(OrderStatus.Refunded, $"Order refunded: {reason}", operatorName);
    }

    private void TransitionTo(OrderStatus nextStatus, string comment, string operatorName)
    {
        var prevStatus = Status;
        Status = nextStatus;
        UpdatedAtUtc = DateTime.UtcNow;

        _statusHistory.Add(new OrderStatusHistory(
            Guid.NewGuid(), Id, prevStatus, nextStatus, comment, operatorName));
    }

    private void RecalculateTotals()
    {
        var subtotalDecimal = _items.Sum(i => i.TotalPrice.Amount);
        var currency = _items.FirstOrDefault()?.UnitPrice.Currency ?? "USD";

        SubTotal = new Money(subtotalDecimal, currency);

        var rawTotal = SubTotal.Amount + ShippingCost.Amount + TaxAmount.Amount - DiscountAmount.Amount;
        TotalAmount = new Money(Math.Max(0m, rawTotal), currency);
    }
}

public sealed class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Sku Sku { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = null!;
    public int Quantity { get; private set; }

    public Money TotalPrice => UnitPrice.Multiply(Quantity);

    private OrderItem() { } // EF Core

    public OrderItem(Guid id, Guid orderId, Guid variantId, Guid productId, string productName, Sku sku, Money unitPrice, int quantity)
        : base(id)
    {
        OrderId = orderId;
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName.Trim();
        Sku = sku ?? throw new ArgumentNullException(nameof(sku));
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        Quantity = quantity;
    }
}

public sealed class OrderStatusHistory : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public OrderStatus? FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public string ChangedBy { get; private set; } = string.Empty;

    private OrderStatusHistory() { } // EF Core

    public OrderStatusHistory(Guid id, Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string comment, string changedBy)
        : base(id)
    {
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Comment = comment.Trim();
        ChangedBy = changedBy.Trim();
    }
}

