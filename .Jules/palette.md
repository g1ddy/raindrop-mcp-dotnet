## 2025-05-18 - Backend Projects and UX

**Learning:** When working on a purely backend project (like an MCP server), the "User Interface" is the text and data returned to the AI or developer. Strict frontend definitions (HTML/CSS) don't apply, but UX principles do.
**Action:** Focus on improving error messages, tool descriptions, and user-facing prompts/confirmations. Treat the AI as the user.

## 2025-05-18 - Configuration Validation UX
**Learning:** For CLI tools, failing fast with a specific, actionable error message is vastly superior to crashing with a stack trace. This is a critical part of the developer experience (DX).
**Action:** When working on CLI tools, always validate critical configuration (like API tokens) before the main application loop and provide clear instructions on how to fix missing values.
