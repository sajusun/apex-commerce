using FluentValidation;
using MediatR;
using ApexCommerce.Application.Common.DTOs.Auth;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
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

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Your account has been deactivated. Please contact support.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new CurrentUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.Role);
        return new AuthResponse(token, refreshToken, "Bearer", 3600, userDto);
    }
}

