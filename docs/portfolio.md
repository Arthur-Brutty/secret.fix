# Portfolio narrative

## Problem

Input-tuning tools often make broad performance claims while hiding what they change. A safer desktop tool should make supported Windows changes observable, reversible, and honest about what it cannot measure.

## Solution and technical approach

`secret.fix` is a .NET 8 WPF prototype that organizes mouse and keyboard settings, HID discovery, input diagnostics, Raw Input timing measurements, profiles, backups, restores, and a conservative game launcher. Small C# services separate state persistence, operation journaling, backup/restore, device discovery, and Win32 calls from the WPF UI. Async device and diagnostic work avoids blocking the UI.

## Architecture and challenges

The project separates presentation, services, state/models, and Windows interop. Profile operations read state, create a backup, apply a supported change, re-read for validation, and retain recovery history. Key challenges include P/Invoke structure translation, avoiding false latency claims, handling local JSON safely, and keeping a public client useful without embedding backend secrets.

## What I learned and next steps

The work demonstrates WPF/XAML composition, .NET service design, P/Invoke boundaries, Raw Input handling, async UI coordination, JSON persistence, xUnit testing, and GitHub Actions automation. Next steps include a separate private licensing API, signed production releases, broader Windows testing, and release-only obfuscation where commercially appropriate. Those capabilities are planned, not presented as complete.
