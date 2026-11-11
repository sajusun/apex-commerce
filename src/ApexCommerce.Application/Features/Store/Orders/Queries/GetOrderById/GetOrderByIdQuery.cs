using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid CustomerId) : IRequest<OrderDto>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        if (order.CustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("You do not have permission to view this order.");

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.PaymentStatus.ToString(),
            order.SubTotal.Amount,
            order.DiscountAmount.Amount,
            order.TaxAmount.Amount,
            order.ShippingCost.Amount,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CouponCode,
            order.ShippingAddress.ToString(),
            order.TrackingNumber,
            order.ShippingCarrier,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemDto(
                i.Id,
                i.VariantId,
                i.ProductName,
                i.Sku.Value,
                i.UnitPrice.Amount,
                i.UnitPrice.Currency,
                i.Quantity,
                i.TotalPrice.Amount
            )).ToList()
        );
    }
}

