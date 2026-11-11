# Multi-Stage Production Dockerfile for ApexCommerce Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Step 1: Copy csproj files and restore dependencies (leveraging Docker layer cache)
COPY ["src/ApexCommerce.Domain/ApexCommerce.Domain.csproj", "src/ApexCommerce.Domain/"]
COPY ["src/ApexCommerce.Application/ApexCommerce.Application.csproj", "src/ApexCommerce.Application/"]
COPY ["src/ApexCommerce.Infrastructure/ApexCommerce.Infrastructure.csproj", "src/ApexCommerce.Infrastructure/"]
COPY ["src/ApexCommerce.Api/ApexCommerce.Api.csproj", "src/ApexCommerce.Api/"]

RUN dotnet restore "src/ApexCommerce.Api/ApexCommerce.Api.csproj"

# Step 2: Copy remaining source code and publish
COPY src/ src/
WORKDIR "/app/src/ApexCommerce.Api"
RUN dotnet publish "ApexCommerce.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Step 3: Lightweight Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApexCommerce.Api.dll"]

