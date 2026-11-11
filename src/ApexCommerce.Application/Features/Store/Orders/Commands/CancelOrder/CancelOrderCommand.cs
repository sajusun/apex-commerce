using MediatR;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Orders.Commands.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, Guid CustomerId, string Reason) : IRequest;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        if (order.CustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("You do not have permission to cancel this order.");

        // Release reserved stock for all items
        var variantIds = order.Items.Select(i => i.VariantId).ToList();
        var variants = (await _productRepository.GetVariantsByIdsAsync(variantIds, cancellationToken))
            .ToDictionary(v => v.Id);

        foreach (var item in order.Items)
        {
            if (variants.TryGetValue(item.VariantId, out var variant))
            {
                variant.ReleaseStock(item.Quantity);
            }
        }

        order.Cancel(request.Reason, "Customer");

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

