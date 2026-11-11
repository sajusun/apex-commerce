namespace ApexCommerce.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Captured = 2,
    Failed = 3,
    Refunded = 4
}

public enum PaymentProvider
{
    Stripe = 1,
    SSLCommerz = 2,
    CashOnDelivery = 3,
    Mock = 4
}

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public enum InventoryChangeReason
{
    InitialStock = 1,
    OrderReservation = 2,
    OrderCancellation = 3,
    ManualRestock = 4,
    DamagedLoss = 5
}

public enum UserRole
{
    Customer = 1,
    StoreManager = 2,
    Admin = 3,
    SuperAdmin = 4
}

