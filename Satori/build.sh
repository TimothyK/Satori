#!/bin/bash
set -e

echo "Starting custom build for .NET 10..."

# Install .NET 10 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --install-dir /tmp/dotnet
export PATH="/tmp/dotnet:$PATH"
export DOTNET_ROOT="/tmp/dotnet"

# Verify .NET version
dotnet --version

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build the project
echo "Building project..."
dotnet build --configuration Release --no-restore

# Publish the project
echo "Publishing project..."
dotnet publish --configuration Release --no-build --output ./bin/Release/publish

echo "Build completed successfully!"