using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ApexCommerce.Application.Common.Exceptions;
using ApexCommerce.Domain.Exceptions;

namespace ApexCommerce.Api.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(validationEx.Errors)
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Validation Failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred.",
                    Instance = context.Request.Path
                }
            ),

            EntityNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Title = "Resource Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = notFoundEx.Message,
                    Instance = context.Request.Path
                }
            ),

            InsufficientStockException stockEx => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Insufficient Inventory",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = stockEx.Message,
                    Instance = context.Request.Path
                }
            ),

            CouponNotApplicableException couponEx => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Coupon Inapplicable",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = couponEx.Message,
                    Instance = context.Request.Path
                }
            ),

            DomainException domainEx => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Title = "Business Rule Violation",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = domainEx.Message,
                    Instance = context.Request.Path
                }
            ),

            UnauthorizedAccessException authEx => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = authEx.Message,
                    Instance = context.Request.Path
                }
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred processing your request.",
                    Instance = context.Request.Path
                }
            )
        };

        response.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }
}

