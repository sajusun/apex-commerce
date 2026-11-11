namespace ApexCommerce.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with identifier '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}

public class InsufficientStockException : DomainException
{
    public Guid VariantId { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }

    public InsufficientStockException(Guid variantId, int requestedQuantity, int availableStock)
        : base($"Insufficient inventory for Product Variant '{variantId}'. Requested: {requestedQuantity}, Available: {availableStock}.")
    {
        VariantId = variantId;
        RequestedQuantity = requestedQuantity;
        AvailableStock = availableStock;
    }
}

public class InvalidOrderStateTransitionException : DomainException
{
    public InvalidOrderStateTransitionException(string currentStatus, string targetStatus)
        : base($"Cannot transition order from state '{currentStatus}' to '{targetStatus}'.") { }
}

public class CouponNotApplicableException : DomainException
{
    public CouponNotApplicableException(string reason) : base(reason) { }
}

