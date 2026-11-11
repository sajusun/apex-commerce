using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;
using ApexCommerce.Domain.ValueObjects;

namespace ApexCommerce.Application.Features.Admin.Catalog.Commands.CreateProduct;

public sealed record CreateVariantItem(
    string Sku,
    string VariantName,
    decimal AdditionalPrice,
    int InitialStock,
    int LowStockThreshold = 5,
    string? Barcode = null
);

public sealed record CreateProductCommand(
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    Guid CategoryId,
    Guid BrandId,
    decimal BasePrice,
    string Currency,
    bool IsFeatured,
    List<CreateVariantItem> Variants
) : IRequest<Guid>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.Variants).NotEmpty().WithMessage("A product must have at least one variant.");
    }
}

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(
            Guid.NewGuid(),
            request.Name,
            request.Slug,
            request.ShortDescription,
            request.Description,
            request.CategoryId,
            request.BrandId,
            new Money(request.BasePrice, request.Currency),
            request.IsFeatured
        );

        foreach (var v in request.Variants)
        {
            product.AddVariant(
                Guid.NewGuid(),
                new Sku(v.Sku),
                v.VariantName,
                new Money(v.AdditionalPrice, request.Currency),
                v.InitialStock,
                v.LowStockThreshold,
                v.Barcode
            );
        }

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}

