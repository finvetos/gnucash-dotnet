# Release Run Model

This directory captures the release contract for this repository.

- `release.run.yml` defines gates, artifacts, and publish defaults.
- `channels/` defines where artifacts may be published.
- `manifests/` records package identity and artifact expectations.

Private NuGet package promotion is local shared directory first, LAN Synology NuGet store second, and Azure Artifacts for released private packages. Public libraries use GitHub and GitHub Releases as the public release surface.

Use `tools/release.ps1` for the executable run. Keep the YAML small and boring so agents can read, compare, and adapt it without needing hidden release knowledge.
