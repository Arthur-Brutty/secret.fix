# Interview notes

## Raw Input and Win32 interop

- `InputBenchmarkService` registers mouse Raw Input and receives `WM_INPUT` through an `HwndSource` hook.
- It uses `Stopwatch` intervals for estimated polling frequency, jitter, and stability, not total system latency.
- Mouse and keyboard services wrap `SystemParametersInfoW`; unmanaged allocation and last-error handling stay in the infrastructure layer.

## WPF state, async work, and recovery

- XAML views focus on interaction while services own system behavior.
- HID discovery and diagnostics use asynchronous work so enumeration does not block the UI.
- Local settings are normalized after deserialization to support additive schema changes.
- Before persistent changes, the app saves a snapshot and operation journal, then re-reads state for validation and supports restore.

## Security and CI/CD

- The license path is a local development mock behind an interface; no production backend exists here.
- Production keys, databases, billing, and admin capabilities remain private.
- Logs redact credential-like values, CI has least-privilege read access, and build output is published as an artifact instead of committed.
