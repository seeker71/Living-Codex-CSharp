# Use the official .NET 6 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy project files
COPY src/CodexBootstrap/CodexBootstrap.csproj src/CodexBootstrap/

# Restore dependencies
RUN dotnet restore src/CodexBootstrap/CodexBootstrap.csproj

# Copy source code
COPY . .

# Build the application
RUN dotnet build src/CodexBootstrap/CodexBootstrap.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish src/CodexBootstrap/CodexBootstrap.csproj -c Release -o /app/publish

# Use the official .NET 6 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

# Install additional dependencies
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app/publish .

# Create directories for HTTPS certificates
RUN mkdir -p /https

# Create directories for logs and data
RUN mkdir -p /app/logs /app/data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

# Expose ports
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "CodexBootstrap.dll"]
