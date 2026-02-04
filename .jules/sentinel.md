## 2024-05-23 - Inconsistent DTO Validation
**Vulnerability:** The `Raindrop` domain entity, used for bulk creation operations, lacked the input validation attributes (`MaxLength`, `Url`) present on the single-item creation DTO (`RaindropCreateRequest`). This created a potential bypass for input limits during bulk operations.
**Learning:** Shared domain entities or DTOs used across different endpoints (single vs. bulk) must maintain consistent validation rules. Reliance on a specific "Request" DTO for validation leaves gaps if the core entity is used directly in other contexts.
**Prevention:** Apply validation attributes directly to the core entity/model when possible, or ensure all input DTOs (including bulk wrappers) enforce the same constraints via shared interfaces or base classes.
