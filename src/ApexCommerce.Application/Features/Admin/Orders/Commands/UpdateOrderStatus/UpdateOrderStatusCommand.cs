using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Admin.Orders.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus TargetStatus,
    string? TrackingNumber,
    string? Carrier,
    string OperatorName
) : IRequest;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OperatorName).NotEmpty();

        When(x => x.TargetStatus == OrderStatus.Shipped, () =>
        {
            RuleFor(x => x.TrackingNumber).NotEmpty().WithMessage("Tracking number is required when setting order status to Shipped.");
            RuleFor(x => x.Carrier).NotEmpty().WithMessage("Carrier is required when setting order status to Shipped.");
        });
    }
}

public sealed class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOrderStatusCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        switch (request.TargetStatus)
        {
            case OrderStatus.Shipped:
                order.MarkAsShipped(request.TrackingNumber!, request.Carrier!, request.OperatorName);
                break;
            case OrderStatus.Delivered:
                order.MarkAsDelivered(request.OperatorName);
                break;
            case OrderStatus.Cancelled:
                order.Cancel("Admin manual cancellation", request.OperatorName);
                break;
            default:
                throw new InvalidOperationException($"Direct transition to status '{request.TargetStatus}' is not permitted via this endpoint.");
        }

        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

