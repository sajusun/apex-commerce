using ApexCommerce.Admin.Blazor;
using ApexCommerce.Application;
using ApexCommerce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Blazor Server Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Register Application (CQRS / MediatR) & Infrastructure (Postgres EF Core / Redis / Services)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// 3. Static files & routing
app.UseStaticFiles();
app.UseAntiforgery();

// 4. Map Root Blazor Component with InteractiveServer render mode (SignalR WebSockets)
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

