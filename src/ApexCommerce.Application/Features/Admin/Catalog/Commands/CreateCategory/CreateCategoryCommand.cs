using FluentValidation;
using MediatR;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Admin.Catalog.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description,
    Guid? ParentCategoryId,
    int DisplayOrder = 0
) : IRequest<Guid>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category(
            Guid.NewGuid(),
            request.Name,
            request.Slug,
            request.Description,
            request.ParentCategoryId,
            request.DisplayOrder
        );

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}

