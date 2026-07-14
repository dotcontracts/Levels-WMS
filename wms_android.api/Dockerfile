# Use smaller base images to reduce memory footprint
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use multi-stage build with optimized SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files first for better layer caching
# Expecting build context at WMS_Android level
RUN echo "=== Starting build process ===" && \
    echo "Working directory: $(pwd)" && \
    echo "Listing build context root:" && \
    ls -la / || echo "Could not list root"

# Copy project files and restore them in their original structure
COPY wms_android.api/wms_android.api.csproj wms_android.api/
COPY wms_android.shared/wms_android.shared.csproj wms_android.shared/
COPY wms_android.cache/wms_android.cache.csproj wms_android.cache/
RUN echo "=== Project files copied ===" && \
    ls -la /src/wms_android.api/ && \
    ls -la /src/wms_android.shared/ && \
    ls -la /src/wms_android.cache/

# Set working directory to the API directory to restore
WORKDIR /src/wms_android.api

# Optimize restore process - reduce memory usage
RUN echo "=== Restoring NuGet packages ===" && \
    dotnet nuget locals all --clear && \
    dotnet restore wms_android.api.csproj \
    --verbosity minimal \
    --no-cache \
    --disable-parallel || (echo "ERROR: Restore failed!" && exit 1)

# Copy source code in original folder structure
WORKDIR /src
COPY wms_android.api/ wms_android.api/
COPY wms_android.shared/ wms_android.shared/
COPY wms_android.cache/ wms_android.cache/
RUN echo "=== Files after copy ===" && \
    ls -la /src/wms_android.api/ | head -20 && \
    ls -la /src/wms_android.shared/ | head -20 && \
    ls -la /src/wms_android.cache/ | head -20

# Build and publish the API project specifically to avoid solution path issues
WORKDIR /src/wms_android.api
RUN echo "Building and publishing application..." && \
    dotnet publish wms_android.api.csproj \
    -c Release \
    -o /app/publish \
    --verbosity minimal \
    --no-cache \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false \
    /p:UseSharedCompilation=false \
    /p:BuildInParallel=false \
    /maxcpucount:1 || (echo "Build failed!" && exit 1)

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "wms_android.api.dll"]
