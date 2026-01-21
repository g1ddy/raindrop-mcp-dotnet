#!/bin/bash
set -e

# Project name (adjust if needed)
PROJECT_NAME=RaindropMcp

# .NET version (here 10.0, adjust if needed)
DOTNET_VERSION=10.0

# Update package sources
sudo apt-get update
sudo apt-get install -y apt-transport-https

# Install .NET SDK from Ubuntu repositories
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
