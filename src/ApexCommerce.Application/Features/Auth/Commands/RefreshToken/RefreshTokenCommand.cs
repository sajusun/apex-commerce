using MediatR;
using ApexCommerce.Application.Common.DTOs.Auth;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var existingToken = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);
        if (existingToken == null || !existingToken.IsActive)
            throw new UnauthorizedAccessException("Refresh token is inactive or revoked.");

        var newAccessToken = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Role);
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.RevokeRefreshToken(request.RefreshToken, newRefreshToken);
        user.AddRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var userDto = new CurrentUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.FullName, user.Role);
        return new AuthResponse(newAccessToken, newRefreshToken, "Bearer", 3600, userDto);
    }
}

