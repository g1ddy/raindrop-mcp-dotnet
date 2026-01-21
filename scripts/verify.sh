#!/bin/bash
set -euo pipefail

# Configuration
SOLUTION_NAME="RaindropMcp.sln"

# 0. Setup
# Call the existing setup script to ensure the environment is ready
# This includes running 'dotnet restore', so we don't need to do it explicitly again.
bash scripts/setup.sh

echo "Setup complete. Starting verification..."

# 1. Check critical versions
dotnet --version || { echo "Dotnet is missing"; exit 1; }

# 2. Dry run a test
# Runs a list of tests without executing them to ensure the test runner and project configuration are valid.
# The --no-restore flag is used because setup.sh has already restored packages.
dotnet test "$SOLUTION_NAME" --list-tests --no-restore || { echo "Test runner failed to start"; exit 1; }

echo "Verification Passed. Environment is healthy."
