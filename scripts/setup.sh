#!/bin/bash
set -e

# Project name (adjust if needed)
PROJECT_NAME=RaindropMcp

# .NET version (here 10.0, adjust if needed)
DOTNET_VERSION=10.0

# Add Microsoft package source with checksum verification
# Targeting Ubuntu 24.04 (Noble) for .NET 10 support
PACKAGE_FILE="packages-microsoft-prod.deb"
EXPECTED_CHECKSUM="c13f01ac7c3001b51a9281d40dde666db5e037e05512840c319832f7852bfec4"
PACKAGE_URL="https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb"

wget "$PACKAGE_URL" -O "$PACKAGE_FILE"

CURRENT_CHECKSUM=$(sha256sum "$PACKAGE_FILE" | awk '{print $1}')

if [ "$CURRENT_CHECKSUM" != "$EXPECTED_CHECKSUM" ]; then
    echo "Error: Checksum verification failed for $PACKAGE_FILE"
    echo "Expected: $EXPECTED_CHECKSUM"
    echo "Got:      $CURRENT_CHECKSUM"
    rm "$PACKAGE_FILE"
    exit 1
fi

sudo dpkg -i "$PACKAGE_FILE"
rm "$PACKAGE_FILE"

# Update package sources and transport layer
sudo apt-get update
sudo apt-get install -y apt-transport-https

# Install .NET SDK
# Note: apt-get update was already run above
sudo apt-get install -y dotnet-sdk-$DOTNET_VERSION

# Test: display version
dotnet --version

# --- IMPORTANT: Restore ALL packages while Internet is available! ---
dotnet restore $PROJECT_NAME.sln

# (Optional: Build and test immediately while Internet is available)
# Commented out to prevent interference with snapshot creation
# dotnet build $PROJECT_NAME.sln --no-restore
# dotnet test $PROJECT_NAME.sln --no-build --no-restore

echo "Setup and pre-restore completed. Container is ready for offline use."
