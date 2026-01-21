#!/bin/bash

# 0. Setup
# Call the existing setup script to ensure the environment is ready
bash scripts/setup.sh

echo "Setup complete. Starting verification..."

# 1. Check critical versions
dotnet --version || { echo "Dotnet is missing"; exit 1; }

# 2. Verify dependencies
# Uses 'dotnet restore' to check if all NuGet packages are resolved and present.
# This ensures the environment is ready for building and testing.
dotnet restore RaindropMcp.sln || { echo "Dotnet restore failed"; exit 1; }

# 3. Dry run a test
# Runs a list of tests without executing them to ensure the test runner and project configuration are valid.
dotnet test RaindropMcp.sln --list-tests --no-restore || { echo "Test runner failed to start"; exit 1; }

echo "Verification Passed. Environment is healthy."
