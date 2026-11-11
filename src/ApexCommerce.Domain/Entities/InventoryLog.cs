using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Domain.Entities;

public sealed class InventoryLog : Entity<Guid>
{
    public Guid VariantId { get; private set; }
    public int QuantityDelta { get; private set; }
    public int StockAfterAdjustment { get; private set; }
    public InventoryChangeReason Reason { get; private set; }
    public string? Note { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private InventoryLog() { } // EF Core

    public InventoryLog(Guid id, Guid variantId, int quantityDelta, int stockAfterAdjustment, InventoryChangeReason reason, string createdBy, string? note = null)
        : base(id)
    {
        VariantId = variantId;
        QuantityDelta = quantityDelta;
        StockAfterAdjustment = stockAfterAdjustment;
        Reason = reason;
        CreatedBy = createdBy.Trim();
        Note = note?.Trim();
    }
}

