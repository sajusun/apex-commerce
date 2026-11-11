using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.DTOs.Auth;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Application.Features.Auth.Commands.Login;
using ApexCommerce.Application.Features.Auth.Commands.RefreshToken;
using ApexCommerce.Application.Features.Auth.Commands.Register;

namespace ApexCommerce.Api.Controllers.Auth;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Role = _currentUserService.Role?.ToString()
        });
    }
}

