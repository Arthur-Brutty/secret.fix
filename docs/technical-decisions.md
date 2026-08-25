# Technical decisions

## WPF and .NET 8

WPF supplies a native Windows UI model, XAML styling, and access to window handles needed for Raw Input. .NET 8 provides current runtime support, nullable reference types, and straightforward test/tooling support.

## Win32 APIs and Raw Input

Mouse and keyboard settings are read and written through documented `SystemParametersInfoW` calls. Raw Input observes mouse events at the window boundary and reports intervals, estimated polling frequency, jitter, and stability. It is not an end-to-end latency measurement and does not promise latency gains.

## Backup before apply

Persistent Windows changes require a known prior state. The application writes a backup and pending-operation journal before a supported change, re-reads settings after application, and retains a restore path.

## Explicit scope limits

Unsupported tweaks are unavailable rather than hidden behind a false-success message. DLL injection, memory manipulation, aim assistance, combat macros, anti-cheat bypasses, and server-rule bypasses are excluded from the project.

## Local persistence and license boundary

Settings, history, and backups persist locally as JSON with atomic replacement and schema normalization. `ILicenseService` separates client UI from future authorization infrastructure; the current implementation is deliberately mock/local-only. A production HTTPS API, database, billing logic, server rules, and private signing keys belong in a private backend.
