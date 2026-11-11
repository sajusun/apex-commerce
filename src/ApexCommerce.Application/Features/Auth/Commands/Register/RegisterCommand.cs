using FluentValidation;
using MediatR;
using ApexCommerce.Application.Common.DTOs.Auth;
using ApexCommerce.Application.Common.Exceptions;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Entities;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber
) : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
            throw new InvalidOperationException($"An account with email '{request.Email}' already exists.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(Guid.NewGuid(), request.Email, passwordHash, request.FirstName, request.LastName, UserRole.Customer, request.PhoneNumber);

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new CurrentUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.Role);
        return new AuthResponse(token, refreshToken, "Bearer", 3600, userDto);
    }
}

