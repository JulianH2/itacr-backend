# Security Policy

## Supported versions

Only the latest release tagged on `main` (deployed to Azure) receives security fixes.

## Reporting a vulnerability

**Do not open a public issue or PR for a security problem.**

1. Contact the maintainers privately on GitHub ([@JulianH2](https://github.com/JulianH2) or [@GuillermoRH19](https://github.com/GuillermoRH19)) or through the team's private channel.
2. Include:
   - A description of the vulnerability and its impact.
   - Steps to reproduce, or a proof of concept (endpoint, request, and response).
   - Affected version, branch, or commit.
3. Do not include real credentials, PINs, tokens, connection strings, or customer data in the report.

We will acknowledge the report within **2 business days** and fix it through a `hotfix/ITACR-<N>-<description>` branch from `main` (see [CONTRIBUTING.md](CONTRIBUTING.md#9-hotfixes)).

## Where secrets live

| Secret                                       | Local development      | Production (Azure)               | CI (GitHub Actions)   |
| -------------------------------------------- | ---------------------- | -------------------------------- | --------------------- |
| SQL Server `sa` password                     | `.env`                 | -                                | -                     |
| Connection string                            | `dotnet user-secrets`  | App Service configuration        | Repository secret     |
| JWT secret, VAPID private key                | `dotnet user-secrets`  | App Service configuration        | -                     |
| Deployment credentials, release-please token | -                      | -                                | Repository secrets    |

None of them is ever committed. `appsettings*.json` only contains non-secret values or empty placeholders.

## If a secret is committed

1. **Rotate the secret immediately.** Removing it from git is not enough; assume it is compromised. Change it where it is used (Azure configuration, GitHub secret, or your local `.env` and user secrets).
2. Remove it from the code or `appsettings*.json` and store it in the right place from the table above.
3. Tell the other team member so both local copies are updated.

## Security practices

See [section 1.7 of the coding standards](docs/CODING_STANDARDS.md#17-security-owasp-top-10-basics) and [section 2.4](docs/CODING_STANDARDS.md#24-security-and-operations).
