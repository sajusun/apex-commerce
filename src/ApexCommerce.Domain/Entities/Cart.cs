using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Domain.Entities;

public sealed class Cart : AggregateRoot<Guid>
{
    private readonly List<CartItem> _items = [];

    public Guid? CustomerId { get; private set; }
    public string? SessionId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public decimal SubTotalAmount => _items.Sum(i => i.TotalPrice.Amount);

    private Cart() { } // EF Core

    public Cart(Guid id, Guid? customerId = null, string? sessionId = null) : base(id)
    {
        if (!customerId.HasValue && string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("A cart must have either a CustomerId or a SessionId.");

        CustomerId = customerId;
        SessionId = sessionId?.Trim();
        ExpiresAtUtc = DateTime.UtcNow.AddDays(14);
    }

    public void AddOrUpdateItem(Guid variantId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Item quantity must be greater than zero.", nameof(quantity));

        var existingItem = _items.FirstOrDefault(i => i.VariantId == variantId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            _items.Add(new CartItem(Guid.NewGuid(), Id, variantId, quantity, unitPrice));
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateItemQuantity(Guid cartItemId, int newQuantity)
    {
        var item = _items.FirstOrDefault(i => i.Id == cartItemId)
            ?? throw new InvalidOperationException($"Cart item '{cartItemId}' does not exist in cart.");

        if (newQuantity <= 0)
        {
            _items.Remove(item);
        }
        else
        {
            item.UpdateQuantity(newQuantity);
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemoveItem(Guid cartItemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item != null)
        {
            _items.Remove(item);
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public void AssignCustomer(Guid customerId)
    {
        CustomerId = customerId;
        SessionId = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Clear()
    {
        _items.Clear();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class CartItem : Entity<Guid>
{
    public Guid CartId { get; private set; }
    public Guid VariantId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;

    public Money TotalPrice => UnitPrice.Multiply(Quantity);

    private CartItem() { } // EF Core

    public CartItem(Guid id, Guid cartId, Guid variantId, int quantity, Money unitPrice) : base(id)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        CartId = cartId;
        VariantId = variantId;
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        Quantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

