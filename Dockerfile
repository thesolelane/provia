# ============================================================
# PROVIA - Dockerfile
# Multi-stage build: SDK for compile, runtime for execution
# Target: .NET 6 ASP.NET Core on Linux
# ============================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy project file first (layer cache: only re-run restore if .csproj changes)
COPY JobTracker/JobTracker.csproj ./JobTracker/
RUN dotnet restore ./JobTracker/JobTracker.csproj

# Copy everything else and publish
COPY JobTracker/ ./JobTracker/
WORKDIR /src/JobTracker
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create uploads directory (will be mounted as a volume in production)
RUN mkdir -p /app/wwwroot/uploads

# Copy published output from build stage
COPY --from=build /app/publish .

# Run as non-root user for security
RUN addgroup --system provia && adduser --system --ingroup provia provia
RUN chown -R provia:provia /app
USER provia

# Expose the app port
EXPOSE 5000

# Health check — hits the lightweight test endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:5000/api/test || exit 1

# Start the app
ENTRYPOINT ["dotnet", "JobTracker.dll", "--urls=http://0.0.0.0:5000"]
