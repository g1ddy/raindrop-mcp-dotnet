## 2025-02-18 - Friendly CLI Configuration Errors
**Learning:** For MCP servers (headless CLI tools), startup errors must be printed to `stderr` and clearly formatted. Users cannot see "UI" popups, so the console error is their only onboarding guide.
**Action:** Always wrap mandatory configuration checks in a pre-flight block that outputs actionable instructions to stderr before the host builder validation runs.
