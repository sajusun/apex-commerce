using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.Entities;

public sealed class Category : Entity<Guid>
{
    private readonly List<Category> _subCategories = [];

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Category> SubCategories => _subCategories.AsReadOnly();

    private Category() { } // EF Core

    public Category(Guid id, string name, string slug, string? description = null, Guid? parentCategoryId = null, int displayOrder = 0)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Category slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        ParentCategoryId = parentCategoryId;
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public void Update(string name, string slug, string? description, Guid? parentCategoryId, int displayOrder, bool isActive)
    {
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        ParentCategoryId = parentCategoryId;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

