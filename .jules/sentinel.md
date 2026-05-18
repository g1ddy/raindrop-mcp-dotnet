## 2024-05-23 - Inconsistent DTO Validation
**Vulnerability:** The `Raindrop` domain entity, used for bulk creation operations, lacked the input validation attributes (`MaxLength`, `Url`) present on the single-item creation DTO (`RaindropCreateRequest`). This created a potential bypass for input limits during bulk operations.
**Learning:** Shared domain entities or DTOs used across different endpoints (single vs. bulk) must maintain consistent validation rules. Reliance on a specific "Request" DTO for validation leaves gaps if the core entity is used directly in other contexts.
**Prevention:** Apply validation attributes directly to the core entity/model when possible, or ensure all input DTOs (including bulk wrappers) enforce the same constraints via shared interfaces or base classes.

## 2024-05-24 - Missing Required Field in Bulk DTO
**Vulnerability:** The `Raindrop` class, used for bulk bookmark creation, did not mark the `Link` property as `[Required]`, allowing the creation of invalid bookmarks via the bulk API, whereas the single-item API correctly enforced this.
**Learning:** Reusing domain objects for DTOs can lead to missing validation if the domain object is designed to be nullable (e.g., for partial updates or responses).
**Prevention:** Explicitly verify that all DTOs used for creation (bulk or single) enforce required fields using `[Required]` attributes, even if the underlying domain model allows nulls for other contexts.

## 2024-05-25 - Missing Validation on Highlight DTOs
**Vulnerability:** The `HighlightCreateRequest` and `HighlightUpdateRequest` DTOs relied on documentation ("This field is required") but lacked the `[Required]` validation attribute, allowing invalid requests (e.g., empty text or ID) to be processed.
**Learning:** Documentation comments do not enforce validation. DTOs exposed to external inputs must explicitly use validation attributes like `[Required]` to guarantee data integrity before processing.
**Prevention:** Audit all request DTOs to ensure that fields described as required are backed by `[Required]` attributes, and include unit tests to verify the presence of these attributes.

## 2024-05-26 - Missing Input Length Limits on Highlight Domain Model
**Vulnerability:** The `Highlight` domain entity, which represents user-created highlights, lacked `[MaxLength]` validation attributes on its `Text` and `Note` properties. These properties represent arbitrary text input from users, posing a risk of denial-of-service (DoS) or unexpected database constraints violations if exceptionally large strings are passed to endpoints that use the base domain entity instead of the request DTOs.
**Learning:** Shared domain entities used across various endpoints (single vs. bulk operations) must maintain consistent data length limits as their corresponding DTOs (e.g., `HighlightCreateRequest` and `HighlightUpdateRequest`). Relying entirely on specialized DTOs for length limits introduces security gaps if the domain model is used elsewhere without limits.
**Prevention:** Always mirror the `[MaxLength(MaxTextFieldLength)]` constraint onto the base domain entity fields whenever it exists on any DTO mapping to those fields.

## 2024-05-27 - Missing Length Limits on Shared Domain Entity
**Vulnerability:** The `Collection` domain entity lacked `[MaxLength]` validation attributes on user-controlled text fields (`Title`, `Description`). If these objects are created or updated using the bulk API, or used as intermediary objects in operations like `SuggestCollectionForBookmarkAsync` that manipulate text sizes in buffers, exceptionally large inputs could lead to Denial of Service (DoS) due to excessive memory consumption.
**Learning:** Shared domain entities used across various endpoints must maintain consistent data length limits. Relying entirely on downstream API validation introduces security gaps when processing or transforming this data locally (e.g., in ArrayPool allocations).
**Prevention:** Apply `[MaxLength]` validation attributes to all text properties in domain models used for user input and data parsing to ensure safe bounds within the local application logic before passing downstream.

## 2024-05-24 - [Enforce HTTPS]
**Vulnerability:** API tokens could be transmitted in plaintext if HTTP was accidentally configured.
**Learning:** We need layered checks (DataAnnotations and runtime validation) when handling sensitive tokens over external networks.
**Prevention:** Always enforce HTTPS scheme validation both at configuration binding and right before HttpClient dispatch for third-party APIs.
## 2024-03-24 - [Information Leakage via Unhandled Exceptions]
**Vulnerability:** The application was exposing internal framework details (stack traces) to stderr when starting without required configurations (like the Raindrop API token).
**Learning:** By default, .NET Generic Host allows configuration validation exceptions to crash the application, resulting in a verbose, generic stack trace that leaks internal code paths and framework versions.
**Prevention:** Implement fail-fast configuration checks early in the application startup pipeline (Program.cs) before the main Host run loop. Catch OptionsValidationException to provide clean, generic error messages and exit gracefully (Environment.Exit), preventing stack trace leaks.

## 2024-05-28 - Missing Validation on Title Fields
**Vulnerability:** Several domain entities (`Raindrop`, `Highlight`) and DTOs (`RaindropCreateRequest`, `RaindropUpdateRequest`) lacked `[MaxLength]` validation attributes on the `Title` property, posing a risk of denial-of-service (DoS) or unexpected database constraints violations if exceptionally large strings are passed. This could lead to excessive memory consumption, particularly when processing or transforming this data locally (e.g., in ArrayPool allocations within `Sanitize`).
**Learning:** Shared domain entities and DTOs must maintain consistent data length limits across all text fields that handle arbitrary user input.
**Prevention:** Always verify and apply `[MaxLength(MaxTextFieldLength)]` constraint onto text properties (like `Title`, `Description`, `Excerpt`, `Note`) in domain models and request DTOs used for user input and data parsing.
## 2024-05-29 - Missing Input Length Limits on Tag Rename DTO
**Vulnerability:** The `TagRenameRequest` DTO lacked a `[MaxLength]` validation attribute on its `Replace` property. Because this property accepts user-provided text to rename tags across all bookmarks, an exceptionally large string could lead to excessive memory consumption and Denial of Service (DoS) when processed by the server or downstream APIs.
**Learning:** All DTO properties that accept arbitrary text input must be explicitly bounded to prevent resource exhaustion attacks.
**Prevention:** Apply `[MaxLength(Raindrops.Raindrop.MaxTextFieldLength)]` from `System.ComponentModel.DataAnnotations` to all string properties in request DTOs to enforce safe limits before application processing.
