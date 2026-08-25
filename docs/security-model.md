# Security model

## Public repository boundary

This repository contains the WPF client, Windows integration, diagnostics, benchmark calculations, backup/restore behavior, public models, tests, CI configuration, and documentation. These components are useful for portfolio review and do not need a production secret.

## Private boundary

Production authentication, license validation, databases, server-side entitlement rules, billing, administrator APIs, provider credentials, private signing keys, and master license material belong in a separate private backend.

The intended trust path is `secret.fix.exe -> HTTPS API -> license database`. A client may contain public DTOs, a public endpoint, and a public verification key. It must never contain a database password, private signing key, JWT signing secret, payment-provider secret, or admin token.

## Local data and logs

Settings, operation history, backups, and logs are stored under the current user's local application-data directory and are not committed. Logs redact credential-like values and should record masked license information only. Local device and path data should be treated as personal data when collecting support material.

## GitHub Actions and cryptography

The current workflow uses no secrets and has read-only repository-content permission. Future signing or deployment values must use GitHub Actions Secrets and must not be printed to logs or artifacts. If offline validation is needed, a server should sign entitlements with an asymmetric private key and the client should verify with the public key. The client must not implement custom cryptography.

## Repository hygiene

`.gitignore` excludes build output, local runtime data, `.env` variants, credentials, certificates, private keys, local databases, and crash dumps. If a real secret is found in Git history, rotate it before considering a history rewrite; do not force-push without an explicit decision.
