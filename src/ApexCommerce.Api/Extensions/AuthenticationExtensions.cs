using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ApexCommerce.Domain.Enums;
using ApexCommerce.Infrastructure.Identity;

namespace ApexCommerce.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAndPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim(ClaimTypes.Role, UserRole.Admin.ToString(), UserRole.SuperAdmin.ToString()));

            options.AddPolicy("ManagerOrAdmin", policy =>
                policy.RequireClaim(ClaimTypes.Role, UserRole.StoreManager.ToString(), UserRole.Admin.ToString(), UserRole.SuperAdmin.ToString()));
        });

        return services;
    }
}

