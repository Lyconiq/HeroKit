# Security policy

## Supported versions

| Version | Supported |
| --- | --- |
| 3.x | Yes |
| 2.x and older | No |

The latest 3.x release gets security fixes. A 3.x line stays supported for at least 12 months after the next major version ships.

## Reporting a vulnerability

Do not open a public issue for a security problem.

Report it privately on GitHub: open the [Security tab](https://github.com/Lyconiq/HeroKit/security) and select **Report a vulnerability**. Include the package and version, a short description and, if possible, a minimal reproduction.

You get a first answer within 7 days. When a fix is ready, we publish a patch release and a GitHub security advisory, and credit you unless you ask us not to.

## Verifying packages

Every package published from this repository has a signed build provenance attestation and an SPDX software bill of materials (SBOM) attached to its GitHub release. To check that a package was built by this repository's workflow:

```bash
gh attestation verify HeroKit.3.6.0.nupkg --repo Lyconiq/HeroKit
```
