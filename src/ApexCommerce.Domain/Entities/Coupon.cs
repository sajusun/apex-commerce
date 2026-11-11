using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Domain.Entities;

public sealed class Coupon : Entity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public DiscountType Type { get; private set; }
    public decimal Value { get; private set; }
    public Money? MinOrderAmount { get; private set; }
    public Money? MaxDiscountAmount { get; private set; }
    public DateTime? StartDateUtc { get; private set; }
    public DateTime? ExpiryDateUtc { get; private set; }
    public int? MaxUsageCount { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Coupon() { } // EF Core

    public Coupon(
        Guid id,
        string code,
        DiscountType type,
        decimal value,
        Money? minOrderAmount = null,
        Money? maxDiscountAmount = null,
        DateTime? startDateUtc = null,
        DateTime? expiryDateUtc = null,
        int? maxUsageCount = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Coupon code is required.", nameof(code));
        if (value <= 0) throw new ArgumentException("Coupon value must be positive.", nameof(value));

        Code = code.Trim().ToUpperInvariant();
        Type = type;
        Value = value;
        MinOrderAmount = minOrderAmount;
        MaxDiscountAmount = maxDiscountAmount;
        StartDateUtc = startDateUtc;
        ExpiryDateUtc = expiryDateUtc;
        MaxUsageCount = maxUsageCount;
        UsedCount = 0;
        IsActive = true;
    }

    public Money CalculateDiscount(Money orderSubtotal)
    {
        if (!IsActive)
            throw new CouponNotApplicableException($"Coupon '{Code}' is inactive.");

        var now = DateTime.UtcNow;
        if (StartDateUtc.HasValue && now < StartDateUtc.Value)
            throw new CouponNotApplicableException($"Coupon '{Code}' has not started yet.");

        if (ExpiryDateUtc.HasValue && now > ExpiryDateUtc.Value)
            throw new CouponNotApplicableException($"Coupon '{Code}' has expired.");

        if (MaxUsageCount.HasValue && UsedCount >= MaxUsageCount.Value)
            throw new CouponNotApplicableException($"Coupon '{Code}' usage limit has been reached.");

        if (MinOrderAmount != null && orderSubtotal.Amount < MinOrderAmount.Amount)
            throw new CouponNotApplicableException($"Order amount ({orderSubtotal}) is below minimum requirement ({MinOrderAmount}) for coupon '{Code}'.");

        decimal discount = Type switch
        {
            DiscountType.Percentage => Math.Round(orderSubtotal.Amount * (Value / 100m), 2),
            DiscountType.FixedAmount => Math.Min(orderSubtotal.Amount, Value),
            _ => 0m
        };

        if (MaxDiscountAmount != null && discount > MaxDiscountAmount.Amount)
        {
            discount = MaxDiscountAmount.Amount;
        }

        return new Money(discount, orderSubtotal.Currency);
    }

    public void IncrementUsage()
    {
        UsedCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

