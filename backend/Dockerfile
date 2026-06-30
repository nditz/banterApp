# syntax=docker/dockerfile:1

# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore as a distinct layer (cached unless the csproj changes)
COPY BanterApp.Api/BanterApp.Api.csproj BanterApp.Api/
RUN dotnet restore BanterApp.Api/BanterApp.Api.csproj

# Copy the rest of the source and publish
COPY BanterApp.Api/ BanterApp.Api/
RUN dotnet publish BanterApp.Api/BanterApp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl is needed for the container HEALTHCHECK
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render injects PORT at runtime; default to 8080 for local runs.
# Program.cs reads PORT and binds Kestrel to http://+:${PORT}.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

# Run as the non-root user that the .NET base image provides.
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -f "http://localhost:${PORT:-8080}/health" || exit 1

ENTRYPOINT ["dotnet", "BanterApp.Api.dll"]
