# Submitting LookUp to winget

These three manifests describe LookUp for the [Windows Package Manager](https://learn.microsoft.com/windows/package-manager/).
Once merged, anyone can install with:

```powershell
winget install DenisDrobyshev.LookUp
```

Package type is **portable**: winget downloads `LookUp.exe`, stores it, and adds a
`lookup` command to PATH. No installer, no admin rights.

## Validate locally

```powershell
winget validate --manifest packaging/winget/1.1.0
winget install --manifest packaging/winget/1.1.0   # optional: test the real install
```

## Open the PR

1. Fork [`microsoft/winget-pkgs`](https://github.com/microsoft/winget-pkgs).
2. Copy the three YAML files into:
   `manifests/d/DenisDrobyshev/LookUp/1.1.0/`
3. Commit, push, open a PR against `microsoft/winget-pkgs`.
   The `wingetcreate`/`komac` tools can also do steps 1–3:
   ```powershell
   komac update DenisDrobyshev.LookUp --version 1.1.0 ^
     --urls https://github.com/DenisDrobyshev/LookUp/releases/download/v1.1.0/LookUp.exe
   ```

## Note on the unsigned binary

The exe isn't code-signed, so the automated validation pipeline may flag it for
manual review (the same SmartScreen/Defender reputation issue). Portable packages
usually clear review, but expect a maintainer to look at it. Keeping the release
URL and SHA-256 stable helps.

## On every new release

Bump `PackageVersion` in all three files and update `InstallerUrl` /
`InstallerSha256` (the release's `SHA256SUMS.txt` has the hash), then open a new PR.
