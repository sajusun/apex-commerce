using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Secret { get; init; } = "ApexCommerceSuperSecretLongKeyForHmacSha256Security1234567890!";
    public string Issuer { get; init; } = "ApexCommerceApi";
    public string Audience { get; init; } = "ApexCommerceClient";
    public int ExpirationInMinutes { get; init; } = 60;
}

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    public string GenerateToken(Guid userId, string email, UserRole role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public UserRole? Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

