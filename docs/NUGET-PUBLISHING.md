# NuGet Publishing Guide

This repository publishes packages to NuGet.org using **Trusted Publishing** with OpenID Connect (OIDC), eliminating the need for long-lived API keys.

## Package Names

All packages are published under the `NEOilx.` prefix to distinguish from the upstream project:

| Package |
|---------|
| NEOilx.IdempotentAPI |
| NEOilx.IdempotentAPI.AccessCache |
| NEOilx.IdempotentAPI.Cache.Abstractions |
| NEOilx.IdempotentAPI.Cache.DistributedCache |
| NEOilx.IdempotentAPI.Cache.FusionCache |
| NEOilx.IdempotentAPI.DistributedAccessLock.Abstractions |
| NEOilx.IdempotentAPI.DistributedAccessLock.MadelsonDistributedLock |
| NEOilx.IdempotentAPI.DistributedAccessLock.RedLockNet |
| NEOilx.IdempotentAPI.MinimalAPI |

## How to Publish a Release

1. Ensure all changes are committed and pushed to `master`
2. Create and push a version tag:
   ```bash
   git tag v2.5.0
   git push origin v2.5.0
   ```
3. The GitHub Actions workflow will automatically:
   - Build the solution
   - Run all tests
   - Pack NuGet packages
   - Publish to GitHub Packages
   - Publish to NuGet.org

## How Trusted Publishing Works

Traditional NuGet publishing requires storing a long-lived API key as a GitHub secret. Trusted Publishing replaces this with short-lived, automatically-rotated credentials using OIDC.

### The Flow

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│  GitHub Actions │──1──▶│     GitHub      │──2──▶│   NuGet.org     │
│    Workflow     │      │  OIDC Provider  │      │                 │
└─────────────────┘      └─────────────────┘      └─────────────────┘
        │                                                  │
        │                        3                         │
        └──────────────────────────────────────────────────┘
```

1. **Workflow requests OIDC token**: When the workflow runs, it requests a signed JWT from GitHub's OIDC provider. This token contains claims about the repository, branch, and workflow.

2. **Token exchange**: The `nuget/login@v1` action sends this token to NuGet.org, which validates:
   - The token is signed by GitHub
   - The repository owner matches the Trusted Publisher policy
   - The repository name matches
   - The workflow file matches

3. **Temporary API key issued**: NuGet.org issues a short-lived API key (~1 hour) that can only push packages. This key is used for the `dotnet nuget push` command.

### Security Benefits

- **No secrets to manage**: No API keys stored in GitHub secrets
- **No secrets to rotate**: Credentials are generated fresh for each run
- **No secrets to leak**: Even if logs are exposed, the temporary key expires quickly
- **Scoped by workflow**: Only the specific workflow file can trigger publishing

### Workflow Configuration

The workflow requires the `id-token: write` permission to request OIDC tokens:

```yaml
publish-nuget:
  runs-on: ubuntu-latest
  if: startsWith(github.ref, 'refs/tags/v')
  permissions:
    id-token: write

  steps:
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 8.0.x

    - uses: actions/download-artifact@v4
      with:
        name: nuget-packages
        path: ${{ env.NuGetDirectory }}

    - uses: nuget/login@v1
      with:
        nuget-server: https://api.nuget.org/v3/index.json

    - run: |
        for package in ${{ env.NuGetDirectory }}/*.nupkg; do
          dotnet nuget push "$package" \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
        done
```

## Setting Up Trusted Publishing (One-Time Setup)

If you need to set up Trusted Publishing for a new NuGet.org account:

1. Sign in at https://www.nuget.org
2. Go to **Account Settings** → **Trusted Publishers**
3. Click **Create** and fill in:
   - **Repository Owner**: `neoilx`
   - **Repository Name**: `IdempotentAPI`
   - **Workflow File**: `nuget-publish.yaml`
   - **Environment**: (leave empty)
4. Click **Create**

For private repositories, the policy is active for 7 days initially. Complete your first publish within that window to make it permanent.

## References

- [Trusted Publishing on NuGet.org](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [GitHub OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [nuget/login Action](https://github.com/nuget/login)
