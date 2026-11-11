using ApexCommerce.Domain.Common;
using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Domain.Entities;

public sealed class User : AggregateRoot<Guid>
{
    private readonly List<RefreshToken> _refreshTokens = [];

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Customer;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    private User() { } // EF Core

    public User(Guid id, string email, string passwordHash, string firstName, string lastName, UserRole role = UserRole.Customer, string? phoneNumber = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName?.Trim() ?? string.Empty;
        Role = role;
        PhoneNumber = phoneNumber?.Trim();
        IsActive = true;
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName.Trim();
        LastName = lastName?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddRefreshToken(string token, DateTime expiresAtUtc)
    {
        _refreshTokens.Add(new RefreshToken(Guid.NewGuid(), Id, token, expiresAtUtc));
    }

    public void RevokeRefreshToken(string token, string? replacedByToken = null)
    {
        var existing = _refreshTokens.FirstOrDefault(t => t.Token == token);
        existing?.Revoke(replacedByToken);
    }
}

public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;

    private RefreshToken() { } // EF Core

    public RefreshToken(Guid id, Guid userId, string token, DateTime expiresAtUtc) : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        IsRevoked = false;
    }

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

