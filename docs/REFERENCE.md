# Technical Reference

This document provides a detailed reference for the technical components of the Raindrop MCP server. For a more hands-on guide, see the [Tutorial](./TUTORIAL.md) or [How-To Guide](./HOW_TO.md).

-   [Back to Home](../README.md)

---

## **Project Structure**

This is a high-level overview of the most important files and directories in the `src/Mcp` project.

-   **`Program.cs`**: The entry point of the application. Configures and runs the MCP host.
-   **`RaindropServiceCollectionExtensions.cs`**: An extension method to register all the Raindrop.io services with the DI container.
-   **`/Collections`, `/Highlights`, etc.:** Each directory contains the models, API interface, and MCP tools for a specific Raindrop.io resource.
-   **`/Common`**: Contains shared models and base classes used across the project.
-   **`appsettings.json`**: Used for configuration, such as API keys.

---

### **MCP Tools**

_**Note:** This section is currently being updated manually. The `scripts/generate-docs.ps1` script is temporarily unavailable and needs to be fixed._

#### `list_collections`
- **Description:** Retrieves all top-level (root) collections. Use this to understand your collection hierarchy before making structural changes.
- **Usage:** `list_collections `

#### `list_child_collections`
- **Description:** Retrieves all nested (child) collections.
- **Usage:** `list_child_collections `

#### `get_collection`
- **Description:** Retrieves a single collection by its unique ID.
- **Usage:** `get_collection [id]`

#### `create_collection`
- **Description:** Creates a new collection. To create a subcollection, include a parent object in the collection parameter.
- **Usage:** `create_collection [collection]`

#### `update_collection`
- **Description:** Updates an existing collection.
- **Usage:** `update_collection [id] [collection]`

#### `delete_collection`
- **Description:** Removes a collection. Bookmarks within it are moved to the Trash.
- **Usage:** `delete_collection [id]`

#### `merge_collections`
- **Description:** Merge multiple collections into a destination collection. Requires both the target collection ID and an array of source collection IDs to merge.
- **Usage:** `merge_collections [to] [ids]`

#### `get_available_filters`
- **Description:** Retrieves available filters for a specific collection or all bookmarks.
- **Usage:** `get_available_filters [collectionId] [search] [tagsSort]`

#### `list_highlights`
- **Description:** Retrieves all highlights across all bookmarks.
- **Usage:** `list_highlights [page] [perPage]`

#### `list_highlights_by_collection`
- **Description:** Retrieves highlights in a specific collection.
- **Usage:** `list_highlights_by_collection [collectionId] [page] [perPage]`

#### `get_bookmark_highlights`
- **Description:** Retrieves all highlights for a specific bookmark.
- **Usage:** `get_bookmark_highlights [raindropId]`

#### `create_highlight`
- **Description:** Adds a new highlight to a bookmark.
- **Usage:** `create_highlight [raindropId] [request]`

#### `update_highlight`
- **Description:** Updates an existing highlight on a bookmark.
- **Usage:** `update_highlight [raindropId] [request]`

#### `delete_highlight`
- **Description:** Removes a highlight from a bookmark.
- **Usage:** `delete_highlight [raindropId] [highlightId]`

#### `list_bookmarks`
- **Description:** Retrieves a list of bookmarks from a specific collection. For large collections, use pagination with perPage=50 to retrieve all bookmarks.
- **Usage:** `list_bookmarks [collectionId] [page] [perPage] [search] [sort] [nested]`

#### `get_bookmark`
- **Description:** Retrieves a single bookmark by its unique ID.
- **Usage:** `get_bookmark [id]`

#### `create_bookmark`
- **Description:** Creates a new bookmark.
- **Usage:** `create_bookmark [request]`

#### `update_bookmark`
- **Description:** Updates an existing bookmark.
- **Usage:** `update_bookmark [id] [request]`

#### `delete_bookmark`
- **Description:** Moves a bookmark to the Trash.
- **Usage:** `delete_bookmark [id]`

#### `update_bookmarks`
- **Description:** Bulk update bookmarks in a collection. For precise targeting, use the ids parameter in the update object.
- **Usage:** `update_bookmarks [collectionId] [update] [search] [nested]`

#### `list_tags`
- **Description:** Retrieves all tags, optionally filtered by a collection.
- **Usage:** `list_tags [collectionId]`

#### `rename_tag`
- **Description:** Renames a tag across all bookmarks.
- **Usage:** `rename_tag [oldTag] [newTag] [collectionId]`

#### `delete_tag`
- **Description:** Removes a tag from all bookmarks.
- **Usage:** `delete_tag [tag] [collectionId]`

#### `get_user_info`
- **Description:** Retrieves the details of the currently authenticated user.
- **Usage:** `get_user_info `

---

## **Configuration (`appsettings.json`)**

| Key                 | Type     | Description                                                                    |
| :------------------ | :------- | :----------------------------------------------------------------------------- |
| `Raindrop:ApiToken` | `string` | **Required.** Your personal API token from the Raindrop.io developer settings. |

---

## **Core Classes & Interfaces**

-   **`ICollectionsApi`**: Interface defining the contract for interacting with the Raindrop.io Collections API.
-   **`CollectionsTools`**: Implements the MCP tools related to collections.
-   **`RaindropToolBase`**: A base class for tool containers, providing common functionality.

For more details on the architecture, see the [Explanation](./EXPLANATION.md) document.
