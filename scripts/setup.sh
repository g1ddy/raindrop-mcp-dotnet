#!/bin/bash
set -e

# Project name (adjust if needed)
PROJECT_NAME=RaindropMcp

# .NET version (here 8.0, adjust if needed)
DOTNET_VERSION=8.0

# Add Microsoft package source
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package sources and transport layer
sudo apt-get update
sudo apt-get install -y apt-transport-https

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-$DOTNET_VERSION

# Test: display version
dotnet --version

# --- IMPORTANT: Restore ALL packages while Internet is available! ---
dotnet restore $PROJECT_NAME.sln

# (Optional: Build and test immediately while Internet is available)
dotnet build $PROJECT_NAME.sln --no-restore
dotnet test $PROJECT_NAME.sln --no-build --no-restore

echo "Setup and pre-restore completed. Container is ready for offline use."
