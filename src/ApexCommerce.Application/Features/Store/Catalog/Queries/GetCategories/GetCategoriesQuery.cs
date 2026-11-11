using MediatR;
using ApexCommerce.Application.Common.DTOs.Store;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Store.Catalog.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cacheService;

    public GetCategoriesQueryHandler(ICategoryRepository categoryRepository, ICacheService cacheService)
    {
        _categoryRepository = categoryRepository;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = "store:categories:tree";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);

                // Build category hierarchy tree
                var rootCategories = categories
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => MapCategoryWithSubcategories(c, categories))
                    .ToList();

                return rootCategories;
            },
            TimeSpan.FromHours(1),
            cancellationToken);
    }

    private static CategoryDto MapCategoryWithSubcategories(Domain.Entities.Category category, IReadOnlyList<Domain.Entities.Category> allCategories)
    {
        var subCategories = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => MapCategoryWithSubcategories(c, allCategories))
            .ToList();

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ParentCategoryId,
            category.DisplayOrder,
            subCategories
        );
    }
}

