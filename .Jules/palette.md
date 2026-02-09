## 2026-02-09 - Improving .NET Host Startup UX
**Learning:** The default `Microsoft.Extensions.Hosting` behavior logs ugly stack traces for configuration errors (e.g., missing options) even before `app.Run()` starts. This is bad for CLI tools where users expect clean error messages.
**Action:** Implement a manual pre-validation step in `Program.cs` before building the host. Use `Validator.TryValidateObject` to check critical configuration and exit gracefully with a helpful message to stderr if invalid.
