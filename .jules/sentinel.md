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
