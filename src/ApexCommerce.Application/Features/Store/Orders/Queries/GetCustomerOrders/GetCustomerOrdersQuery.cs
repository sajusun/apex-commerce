using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Orders.Queries.GetCustomerOrders;

public sealed record GetCustomerOrdersQuery(Guid CustomerId) : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetCustomerOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return orders.Select(order => new OrderDto(
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
        )).ToList();
    }
}

