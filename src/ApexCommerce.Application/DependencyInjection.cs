using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ApexCommerce.Application.Common.Behaviors;

namespace ApexCommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Register Pipeline Behaviors in exact lifecycle order
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Register FluentValidation Validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

