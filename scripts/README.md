# Jules VM Setup Scripts

This directory contains scripts for configuring the development environment in Jules.

## Setup Script (`setup.sh`)

The `setup.sh` script is designed to initialize the Jules VM environment by installing necessary dependencies (like the .NET SDK) and restoring project packages.

### How to Use

1.  This project is configured to use a pre-baked VM snapshot.
2.  If you need to recreate the environment or update the configuration, use the contents of `scripts/setup.sh` as the "Initial Setup" script in the Jules Configuration.
3.  For more details on setting up a Jules VM, refer to the [Jules Environment Documentation](https://jules.google/docs/environment/).

The script performs the following:
*   Installs .NET 8.0 SDK.
*   Restores NuGet packages for the solution.
*   (Optional) Builds and tests the solution to ensure readiness (commented out by default).
