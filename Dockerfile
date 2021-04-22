# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# Dockerfile for SQLite Multi-Tenant
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files
COPY ["src/SqliteMultiTenant.csproj", "src/"]
COPY . .

# Restore dependencies
RUN dotnet restore "src/SqliteMultiTenant.csproj"

# Build application
RUN dotnet build "src/SqliteMultiTenant.csproj" -c Release -o /app/build

# Publish application
RUN dotnet publish "src/SqliteMultiTenant.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create required directories
RUN mkdir -p databases backups logs

# Copy published application
COPY --from=build /app/publish .

# Set permissions
RUN chmod -R 755 .

# Create non-root user
RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5000/api/admin/health || exit 1

# Volume for persistent data
VOLUME ["/app/databases", "/app/backups", "/app/logs"]

# Port
EXPOSE 5000

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

# Start application
ENTRYPOINT ["dotnet", "SqliteMultiTenant.dll"]
