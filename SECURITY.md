# Security Policy

## Supported versions

Only the latest release tagged on `main` receives security fixes.

## Reporting a vulnerability

**Do not open a public issue or PR for a security problem.**

1. Contact the maintainers privately on GitHub ([@JulianH2](https://github.com/JulianH2) or [@GuillermoRH19](https://github.com/GuillermoRH19)) or through the team's private channel.
2. Include:
   - A description of the vulnerability and its impact.
   - Steps to reproduce, or a proof of concept.
   - Affected version, branch, or commit.
3. Do not include real credentials, PINs, tokens, or customer data in the report.

We will acknowledge the report within **2 business days** and fix it through a `hotfix/ITACR-<N>-<description>` branch from `main` (see [CONTRIBUTING.md](CONTRIBUTING.md#9-hotfixes)).

## If a secret is committed

1. **Rotate the secret immediately.** Removing it from git is not enough; assume it is compromised. This includes `JWT_SECRET`, `VAPID_PRIVATE_KEY`, and database passwords.
2. Remove it from the code, move it to `.env`, and add a placeholder to `.env.example`.
3. Tell the other team member so both local copies are updated.

## Security practices

See [section 1.7 of the coding standards](docs/CODING_STANDARDS.md#17-security-owasp-top-10-basics).
