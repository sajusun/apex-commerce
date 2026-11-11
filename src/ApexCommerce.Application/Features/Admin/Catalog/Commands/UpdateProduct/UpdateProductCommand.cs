using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.Features.Admin.Catalog.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    Guid CategoryId,
    Guid BrandId,
    decimal BasePrice,
    string Currency,
    bool IsActive,
    bool IsFeatured
) : IRequest;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new EntityNotFoundException("Product", request.ProductId);

        product.Update(
            request.Name,
            request.Slug,
            request.ShortDescription,
            request.Description,
            request.CategoryId,
            request.BrandId,
            new Money(request.BasePrice, request.Currency),
            request.IsActive,
            request.IsFeatured
        );

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

