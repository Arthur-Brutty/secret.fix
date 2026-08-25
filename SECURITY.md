# Security policy

## Reporting a vulnerability

Please do not publish sensitive vulnerability details, credentials, tokens, or proof-of-concept material in a public issue. Use GitHub private vulnerability reporting when it is available for this repository. If it is not available, contact the repository owner privately through the GitHub profile and include a minimal reproduction, affected version, and impact.

## Secret-handling policy

- Never commit `.env` files, credentials, private keys, certificates, database connection strings, production tokens, or master license keys.
- Use GitHub Actions Secrets for future signing, deployment, or integration credentials. Do not print secret values in workflow logs.
- Treat anything embedded in a desktop client as recoverable by an end user; critical authorization and signing secrets belong in a private backend.
- Development mocks and public placeholders must not be presented as production authentication.

## Supported versions

The actively developed version on `main` is the version reviewed for security fixes. Historical prototype builds are retained only as project history and may not receive updates.
