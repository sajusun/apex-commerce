using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Admin.Inventory.Commands.AdjustStock;

public sealed record AdjustStockCommand(
    Guid VariantId,
    int QuantityDelta,
    InventoryChangeReason Reason,
    string OperatorName,
    string? Note = null
) : IRequest;

public interface IInventoryLogRepository
{
    Task AddAsync(InventoryLog log, CancellationToken cancellationToken);
}

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.QuantityDelta).NotEqual(0).WithMessage("Stock delta cannot be 0.");
        RuleFor(x => x.OperatorName).NotEmpty();
    }
}

public sealed class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryLogRepository _inventoryLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustStockCommandHandler(
        IProductRepository productRepository,
        IInventoryLogRepository inventoryLogRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryLogRepository = inventoryLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ProductVariant), request.VariantId);

        variant.AdjustStock(request.QuantityDelta, request.Reason.ToString());

        var log = new InventoryLog(
            Guid.NewGuid(),
            variant.Id,
            request.QuantityDelta,
            variant.AvailableStock,
            request.Reason,
            request.OperatorName,
            request.Note
        );

        await _inventoryLogRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

