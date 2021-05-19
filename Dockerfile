# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# Dockerfile for SQLite Multi-Tenant
# =====================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/SqliteMultiTenant.csproj", "src/"]

# Copy solution and global files
COPY *.sln ./
COPY global.json ./
COPY Directory.Build.props ./

# Restore dependencies
RUN dotnet restore "src/SqliteMultiTenant.csproj"


# Copy source code
COPY src/ src/

# Build application
RUN dotnet build "src/SqliteMultiTenant.csproj" -c Release -o /app/build


# Publish application
RUN dotnet publish "src/SqliteMultiTenant.csproj" -c Release -o /app/publish \
    --no-restore \
    --no-build \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=true

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

# Install curl for health checks and other utilities
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Create required directories with proper permissions
RUN mkdir -p databases backups logs && \
    chown -R 1001:1001 /app/databases && \
    chown -R 1001:1001 /app/backups && \
    chown -R 1001:1001 /app/logs

# Copy published application
COPY --from=build /app/publish .

# Set permissions
RUN chmod -R 755 . && \
    find . -type f -name "*.dll" -exec chmod 644 {} \;

# Create non-root user
RUN useradd -m -u 1001 appuser
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Volume for persistent data
VOLUME ["/app/databases", "/app/backups", "/app/logs"]

# Port - Updated to 8080 to match docker-compose.yml
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_USE_POLLING_FILE_WATCHER=1

# Start application
ENTRYPOINT ["dotnet", "SqliteMultiTenant.dll"]