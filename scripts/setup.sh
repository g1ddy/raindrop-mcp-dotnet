#!/bin/bash
set -e

# Project name (adjust if needed)
PROJECT_NAME=RaindropMcp

# .NET version (here 8.0, adjust if needed)
DOTNET_VERSION=8.0

# Add Microsoft package source with checksum verification
PACKAGE_FILE="packages-microsoft-prod.deb"
EXPECTED_CHECKSUM="0d335c06ceb3227e330a36a7135997207843c726e42ec341e34a3825f151213b"

wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O "$PACKAGE_FILE"

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
