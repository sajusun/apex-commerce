using Serilog;
using ApexCommerce.Api.Extensions;
using ApexCommerce.Api.Middleware;
using ApexCommerce.Application;
using ApexCommerce.Application.Common.Interfaces;
using ApexCommerce.Infrastructure;
using ApexCommerce.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Structured Logging
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console();
});

// 2. Register Application & Infrastructure Layers (Service Providers)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Security & Policies
builder.Services.AddJwtAndPolicies(builder.Configuration);

// 4. OpenAPI / Swagger Documentation
builder.Services.AddSwaggerWithDualDocs();

// 5. Middleware & Controllers
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddControllers();

// 6. CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 7. Global Exception Handling Middleware (RFC 7807)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 8. Serilog Request Logging
app.UseSerilogRequestLogging();

// 9. Interactive Swagger UI (Serving Storefront and Admin Dual Docs)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/store-v1/swagger.json", "Storefront Customer API v1");
        c.SwaggerEndpoint("/swagger/admin-v1/swagger.json", "Admin Back-Office API v1");
        c.RoutePrefix = string.Empty; // Interactive docs served directly at root /
    });
}

// 10. CORS & Security
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// 11. Endpoint Routing
app.MapControllers();

// 12. Auto-Migration & Seed Data
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbInitializer.SeedAsync(dbContext, passwordHasher);
        logger.LogInformation("Database auto-migration and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database auto-migration skipped. Ensure PostgreSQL is accessible.");
    }
}

app.Run();

