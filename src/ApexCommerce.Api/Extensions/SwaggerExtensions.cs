using Microsoft.OpenApi;

namespace ApexCommerce.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithDualDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // Document 1: Storefront API
            c.SwaggerDoc("store-v1", new OpenApiInfo
            {
                Title = "ApexCommerce Storefront API",
                Version = "v1",
                Description = "Customer-facing eCommerce API: Product catalog, cart, checkout, payments, customer orders, and reviews."
            });

            // Document 2: Admin Dashboard API
            c.SwaggerDoc("admin-v1", new OpenApiInfo
            {
                Title = "ApexCommerce Admin Back-Office API",
                Version = "v1",
                Description = "Administrative API for e-commerce back-office: Sales analytics, product management, order fulfillment, inventory audits, and customer LTV."
            });

            // Dual doc inclusion logic
            c.DocInclusionPredicate((docName, apiDesc) =>
            {
                var relativePath = apiDesc.RelativePath?.ToLowerInvariant() ?? string.Empty;

                if (docName == "store-v1")
                {
                    return relativePath.Contains("api/v1/store") || relativePath.Contains("api/v1/auth");
                }

                if (docName == "admin-v1")
                {
                    return relativePath.Contains("api/v1/admin") || relativePath.Contains("api/v1/auth");
                }

                return false;
            });

            // JWT Security Definition
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Paste your token (WITHOUT 'Bearer ' prefix).",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", null, null),
                    new List<string>()
                }
            });
        });

        return services;
    }
}

