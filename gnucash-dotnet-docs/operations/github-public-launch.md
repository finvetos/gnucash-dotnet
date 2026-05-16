# GitHub Public Launch Settings

Use this checklist before making the repository public.

## Current Local State

The local repository currently has no `origin` remote, and GitHub CLI authentication is not usable in this environment. Live GitHub settings could not be inspected or changed from this checkout.

## Recommended Pre-Release Settings

Set the repository to public only after these controls are in place:

1. Disable or restrict community entry points until the first release.
2. Keep `main` protected and use `dev` for active implementation work.
3. Require CI before merging to `main`.
4. Keep release publishing tag-only.
5. Keep GnuCash itself out of this repository and out of packages.

## Feature Settings

In `Settings > General > Features`, use this pre-release posture:

| Feature | Pre-release setting | Reason |
|---|---:|---|
| Issues | Disabled | Avoid public support before a usable release. |
| Discussions | Disabled | Avoid community threads before the API exists. |
| Wiki | Disabled | Keep docs in `gnucash-dotnet-docs/`. |
| Projects | Disabled | Keep roadmap private until triage is ready. |
| Pull requests | Collaborators only, or disabled | Keep implementation focused until the first release. |

If the repository already exists and GitHub CLI is authenticated, the feature settings that are exposed through the repository API can be applied with:

```powershell
gh api `
  -X PATCH repos/finvetos/gnucash-dotnet `
  -f has_issues=false `
  -f has_projects=false `
  -f has_wiki=false
```

Discussions, pull request access, and commit comments may need to be configured in the GitHub web UI depending on the API support available to the account.

## Interaction Limits

For public pre-release visibility, set repository interaction limits to collaborators only:

```powershell
gh api `
  -X PUT repos/finvetos/gnucash-dotnet/interaction-limits `
  -f limit=collaborators_only `
  -f expiry=six_months
```

GitHub interaction limits are temporary. Renew them if the first release is more than six months away, or remove them when community intake opens.

## Commit Comments

Disable commit comments for the repository when the setting is available. This keeps review and support discussion out of individual commit pages before the project is ready for public feedback.

For personal-account repositories, GitHub also supports a user-level default for commit comments, but repository-level settings can override it.

## Local Files That Support This Posture

- `.github/ISSUE_TEMPLATE/config.yml` disables blank issue creation for non-maintainers and points visitors to contribution/security guidance.
- `.github/PULL_REQUEST_TEMPLATE.md` sets expectations for pre-release pull requests.
- `How to Contribute.md` explains the current contribution status.
- `SECURITY.md` keeps security-sensitive reports out of public threads.

These files do not replace repository settings. They only set expectations and shape the UI once Issues or PRs are available.
