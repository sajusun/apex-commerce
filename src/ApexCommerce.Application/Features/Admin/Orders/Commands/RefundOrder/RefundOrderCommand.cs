using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Admin.Orders.Commands.RefundOrder;

public sealed record RefundOrderCommand(
    Guid OrderId,
    string Reason,
    string OperatorName,
    bool RestockItems = true
) : IRequest;

public sealed class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.OperatorName).NotEmpty();
    }
}

public sealed class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefundOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RefundOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        order.Refund(request.Reason, request.OperatorName);

        if (request.RestockItems)
        {
            var variantIds = order.Items.Select(i => i.VariantId).ToList();
            var variants = (await _productRepository.GetVariantsByIdsAsync(variantIds, cancellationToken))
                .ToDictionary(v => v.Id);

            foreach (var item in order.Items)
            {
                if (variants.TryGetValue(item.VariantId, out var variant))
                {
                    variant.AdjustStock(item.Quantity, "RefundRestock");
                }
            }
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

