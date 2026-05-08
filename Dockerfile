# Phase 10.1 / 10.3 — Dockerfile
# Mega Fintrade Risk Monitor .NET
#
# This Dockerfile builds and packages the ASP.NET Core risk monitor service.
# It uses a multi-stage build:
# 1. SDK image for restore/build/publish
# 2. ASP.NET runtime image for the final container
#
# Important:
# The Docker image only needs the main web application.
# The test project is handled by GitHub Actions CI, not by the runtime image.

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy the main application project file first.
# We restore the application project, not the full solution, because the solution
# also references MegaFintradeRiskMonitor.Tests.
COPY MegaFintradeRiskMonitor.csproj ./

# Restore dependencies for the main application only.
RUN dotnet restore MegaFintradeRiskMonitor.csproj

# Copy the full source code.
COPY . ./

# Publish the main application into a clean output folder.
RUN dotnet publish MegaFintradeRiskMonitor.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# The application will listen on port 8080 inside the container.
# docker run or docker-compose can map host port 5189 to container port 8080.
ENV ASPNETCORE_URLS=http://+:8080

# Use appsettings.Docker.json inside the container.
# This keeps normal local dotnet run behavior unchanged.
ENV ASPNETCORE_ENVIRONMENT=Docker

# Create a data folder for SQLite database files.
# In Phase 10.5, this folder will be connected to a Docker volume.
RUN mkdir -p /app/data

# Copy the published application from the build stage.
COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "MegaFintradeRiskMonitor.dll"]