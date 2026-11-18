# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, in its own layer: it only reruns when a project file changes.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/SqlServerMcp.Core/SqlServerMcp.Core.csproj src/SqlServerMcp.Core/
COPY src/SqlServerMcp.Infrastructure/SqlServerMcp.Infrastructure.csproj src/SqlServerMcp.Infrastructure/
COPY src/SqlServerMcp.Server/SqlServerMcp.Server.csproj src/SqlServerMcp.Server/
RUN dotnet restore src/SqlServerMcp.Server/SqlServerMcp.Server.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/SqlServerMcp.Server/SqlServerMcp.Server.csproj \
    --configuration Release --no-restore --output /app/publish

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as the non-root user that the official .NET images provide.
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SqlServerMcp.Server.dll"]
