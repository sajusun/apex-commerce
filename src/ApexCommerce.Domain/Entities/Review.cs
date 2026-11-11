using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.Entities;

public sealed class Review : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public int Rating { get; private set; } // 1 - 5
    public string Title { get; private set; } = string.Empty;
    public string Comment { get; private set; } = string.Empty;
    public bool IsVerifiedPurchase { get; private set; }
    public bool IsApproved { get; private set; } = true;

    private Review() { } // EF Core

    public Review(Guid id, Guid productId, Guid customerId, string customerName, int rating, string title, string comment, bool isVerifiedPurchase = false)
        : base(id)
    {
        if (rating is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(comment)) throw new ArgumentException("Comment is required.", nameof(comment));

        ProductId = productId;
        CustomerId = customerId;
        CustomerName = customerName.Trim();
        Rating = rating;
        Title = title?.Trim() ?? string.Empty;
        Comment = comment.Trim();
        IsVerifiedPurchase = isVerifiedPurchase;
        IsApproved = true;
    }

    public void SetApproved(bool isApproved)
    {
        IsApproved = isApproved;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

