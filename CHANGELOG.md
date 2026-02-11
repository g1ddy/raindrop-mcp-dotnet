# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Security
- **Input Validation**: Enforced `[Required]` validation on the `Link` property of `Raindrop` objects to prevent the creation of invalid bookmarks during bulk operations.

## [v0.1.5-beta] - 2026-01-21

### Added
- **Enhanced User Info Model**: The `UserInfo` response now uses strongly-typed records for `UserConfig`, `DropboxInfo`, and `FilesInfo`, replacing generic objects for better type safety and developer experience.
- **Resilience**: Implemented Polly retry policies to automatically handle transient API errors (HTTP 429 and 5xx series) with exponential backoff.
- **VCR Integration Tests**: Introduced a comprehensive VCR-based integration testing framework. This allows recording and replaying API interactions, significantly improving test reliability and performance while preventing regression.

### Changed
- **MCP SDK Update**: Updated `SuggestCollectionForBookmark` and `DeleteTag` tools to use the concrete `McpServer` class and compliant schema types (`UntitledSingleSelectEnumSchema`), ensuring compatibility with the latest Model Context Protocol SDK (v0.6.0+).
- **Refactoring**: Renamed `IRaindropData` to `IRaindropRequest` and refined `RaindropBulkUpdate` to support flexible media attachment handling.
- **Test Coverage**: Expanded integration test coverage for Collections, Filters, Highlights, Raindrops, Tags, and User endpoints.

## [v0.1.4-beta] - 2026-01-19

### Added
- **Automated Publishing**: Established the first fully automated publishing pipeline using GitHub Actions, ensuring consistent and reliable package delivery to NuGet.org and GitHub Packages.

### Changed
- **Integration Testing**: Significant enhancements to the integration testing suite, laying the groundwork for more robust quality assurance (related to `integration-tests` merge).

## [v0.1.3-beta] - 2025-08-20

### Added
- **Elicitation Support**: Introduced user elicitation capabilities, allowing tools to request confirmation or additional input from the user (e.g., for destructive actions or choices).

## [v0.1.2-beta] - 2025-07-20

### Fixed
- **Bug Fixes**: Addressed various bugs to improve stability and performance (based on historical estimates).

## [v0.1.1-beta] - 2025-07-20

### Added
- **Initial Release**: The first public beta release of the Raindrop MCP server, enabling AI models to interact with Raindrop.io bookmarks, collections, and tags.
