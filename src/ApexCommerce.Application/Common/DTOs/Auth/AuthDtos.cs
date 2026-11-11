using ApexCommerce.Domain.Enums;

namespace ApexCommerce.Application.Common.DTOs.Auth;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresInSeconds, CurrentUserDto User);
public sealed record CurrentUserDto(Guid Id, string Email, string FirstName, string LastName, string FullName, UserRole Role);

