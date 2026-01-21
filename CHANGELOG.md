# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v0.1.5-beta] - 2026-01-21

### Added
- **Enhanced User Info Model**: The `UserInfo` response now uses strongly-typed records for `UserConfig`, `DropboxInfo`, and `FilesInfo`, replacing generic objects for better type safety and developer experience.
- **Resilience**: Implemented Polly retry policies to automatically handle transient API errors (HTTP 429 and 5xx series) with exponential backoff.
- **VCR Integration Tests**: Introduced a comprehensive VCR-based integration testing framework. This allows recording and replaying API interactions, significantly improving test reliability and performance while preventing regression.

### Changed
- **MCP SDK Update**: Updated `SuggestCollectionForBookmark` and `DeleteTag` tools to use the concrete `McpServer` class and compliant schema types (`UntitledSingleSelectEnumSchema`), ensuring compatibility with the latest Model Context Protocol SDK (v0.6.0+).
- **Refactoring**: Renamed `IRaindropData` to `IRaindropRequest` and refined `RaindropBulkUpdate` to support flexible media attachment handling.
- **Test Coverage**: Expanded integration test coverage for Collections, Filters, Highlights, Raindrops, Tags, and User endpoints.
