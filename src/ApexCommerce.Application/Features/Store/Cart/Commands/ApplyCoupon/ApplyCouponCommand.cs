using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.Features.Store.Cart.Commands.ApplyCoupon;

public sealed record ApplyCouponCommand(
    string CouponCode,
    decimal CartSubtotal,
    string Currency = "USD"
) : IRequest<CouponValidationResult>;

public sealed record CouponValidationResult(
    bool IsValid,
    string Code,
    decimal DiscountAmount,
    string Currency,
    string? Message
);

public sealed class ApplyCouponCommandValidator : AbstractValidator<ApplyCouponCommand>
{
    public ApplyCouponCommandValidator()
    {
        RuleFor(x => x.CouponCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CartSubtotal).GreaterThan(0);
    }
}

public sealed class ApplyCouponCommandHandler : IRequestHandler<ApplyCouponCommand, CouponValidationResult>
{
    private readonly ICouponRepository _couponRepository;

    public ApplyCouponCommandHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    public async Task<CouponValidationResult> Handle(ApplyCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, cancellationToken);
        if (coupon == null)
        {
            return new CouponValidationResult(false, request.CouponCode, 0m, request.Currency, "Coupon code not found.");
        }

        try
        {
            var discount = coupon.CalculateDiscount(new Money(request.CartSubtotal, request.Currency));
            return new CouponValidationResult(true, coupon.Code, discount.Amount, discount.Currency, "Coupon applied successfully.");
        }
        catch (CouponNotApplicableException ex)
        {
            return new CouponValidationResult(false, coupon.Code, 0m, request.Currency, ex.Message);
        }
    }
}

