# Raindrop Model Analysis

This document outlines the mapping between the Raindrop API Documentation and the actual API Response (verified via VCR), highlighting discrepancies.

## Source of Truth
- **Documentation**: [Raindrop API v1 - Raindrops](https://developer.raindrop.io/v1/raindrops)
- **VCR Recording**: `tests/Mcp.Tests/Integration/Fixtures/RaindropsTests/Crud.json`

## Main Fields

| Field | Type (Docs) | Type (VCR/Actual) | Notes |
|-------|-------------|-------------------|-------|
| `_id` | Integer | Integer | Mapped to `Id`. |
| `collection` | Object | Object | Mapped to `IdRef`. Docs only mention `$id`. VCR contains `$ref` and `oid` as well. |
| `collection.$id` | Integer | Integer | Mapped to `IdRef.Id`. |
| `cover` | String | String | URL of cover image. |
| `created` | String | String (ISO 8601) | Mapped to `DateTime?`. VCR: `"2026-01-20T06:37:42.823Z"`. |
| `domain` | String | String | Hostname. |
| `excerpt` | String | String | Max length 10000. |
| `note` | String | String | Max length 10000. |
| `lastUpdate` | String | String (ISO 8601) | Mapped to `DateTime?`. |
| `link` | String | String | URL. |
| `media` | Array\<Object\> | Array | Docs: `[ {"link":"url"} ]`. VCR: `[]`. Code had `Type`, but docs don't list it. |
| `tags` | Array\<String\> | Array\<String\> | List of tags. |
| `title` | String | String | Max length 1000. |
| `type` | String | String | "link", "article", etc. |
| `user` | Object | Object | Mapped to `IdRef`. |
| `user.$id` | Integer | Integer | Mapped to `IdRef.Id`. |

## Other Fields

| Field | Type (Docs) | Type (VCR/Actual) | Notes |
|-------|-------------|-------------------|-------|
| `broken` | Boolean | - | Marked as broken. |
| `cache` | Object | - | Cache details. |
| `cache.status` | String | - | |
| `cache.size` | Integer | - | |
| `cache.created` | String | - | |
| `creatorRef` | Object | Object | |
| `creatorRef._id` | Integer | Integer | |
| `creatorRef.fullName` | String | **Missing in VCR** | VCR has `name` instead of `fullName`. **Action**: Excluded `CreatorRef` due to serialization issues in `CreateManyAsync` request building. |
| `file` | Object | - | Uploaded file info. |
| `file.name` | String | - | |
| `file.size` | Integer | - | |
| `file.type` | String | - | Mime type. |
| `important` | Boolean | Boolean/Null | Mapped to `bool?`. VCR shows `false` and `null`. |
| `highlights` | Array | Array | List of highlights. |
| `reminder` | Object | - | |
| `reminder.date` | Date | - | |

## Undocumented / Extra Fields in VCR
These fields appear in the VCR response but are NOT in the documentation tables. They will **NOT** be mapped to the `Raindrop` model, adhering to strict documentation compliance.

- `collectionId`: Integer (e.g., `-1`). Convenience field. **Action**: Remove from `Raindrop` model.
- `sort`: Integer.
- `highlights` (singular object): VCR sometimes contains a `highlight` object in search results.
- `removed`: Boolean.
- `__v`: Version key.
- `creatorRef.name`: VCR has this, Docs have `fullName`.
- `media.type`: Not in docs.

## Implementation Plan
1.  **Remove**: `CollectionId` from `Raindrop`.
2.  **Add**: `Type`, `Cover`, `Media`, `Created`, `LastUpdate`, `Domain`, `Broken`, `Cache`, `CreatorRef`, `File`, `Highlights`, `Reminder`.
3.  **Update**: `MediaItem` to remove `Type`.
4.  **Create**: New records for nested objects (`RaindropCache`, `RaindropCreatorRef`, etc.).
