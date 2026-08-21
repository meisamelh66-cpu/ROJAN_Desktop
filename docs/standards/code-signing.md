# Code Signing

**Status:** Hooks ready, no certificate purchased. Desktop Productionization Sprint 2.

## 1. Current state

`ROJAN Reception Setup.exe` is unsigned today. Windows SmartScreen will
warn a real user on first run ("Windows protected your PC" / "Unknown
Publisher"). This is a real, unresolved gap — not worked around — because
no code-signing certificate exists to sign with in this environment, and
purchasing one was explicitly out of scope for this sprint.

Everything else is ready: the moment a real certificate exists, signing
is a parameter, not a redesign.

## 2. What certificate is required

An **Authenticode code-signing certificate** for a Windows desktop `.exe`
— not a TLS/SSL certificate (already handled separately, server-side, by
ROJAN_Backend's own Let's Encrypt/Certbot setup).

Two options, both work with everything this repo already has wired:

| Type | SmartScreen behavior | Relative cost |
|---|---|---|
| **OV (Organization Validation)** | Builds reputation over time — early installs still warn until enough users have run it | Lower |
| **EV (Extended Validation)** | Immediate SmartScreen trust, no reputation-building period, but requires the private key live on a hardware token (USB HSM) — cannot be stored as a plain `.pfx` file, which changes the CI signing approach below (a CI runner would need the hardware token attached, or the CA's cloud-HSM signing service instead of local `signtool.exe /f`) | Higher |

Recommendation for a real salon-facing product where first-run trust
matters: **EV**, specifically because it removes the SmartScreen warning
immediately rather than over weeks of accumulated installs — but this is
a purchasing decision for whoever owns that budget, not something this
sprint decides.

Either way, request the certificate from a CA on Microsoft's trusted
list (DigiCert, Sectigo, SSL.com, GlobalSign, etc.) in the name of the
actual legal entity behind ROJAN — Authenticode certificates are
identity-verified, not just domain-verified like a TLS cert, so this
takes real business documentation and typically a few business days.

## 3. Local signing (once a certificate exists)

`build/publish-installer.ps1` already accepts:

```powershell
.\build\publish-installer.ps1 `
    -CertificatePath "C:\path\to\cert.pfx" `
    -CertificatePassword "the-private-key-password" `
    -TimestampUrl "http://timestamp.digicert.com"   # optional, this is already the default
```

What this does:
1. Signs `publish\Rojan.Desktop.Shell.exe` directly via `signtool.exe`
   (auto-detected under `Windows Kits\10\bin`, or pass `-SignToolPath`)
   **before** Inno Setup packages it — the exe inside the installer is
   signed, not just the installer wrapper.
2. Passes Inno Setup a `SignInstaller` preprocessor flag and a named
   `signtool` tool definition (via ISCC's `/S` switch), which
   `RojanReception.iss`'s `[Setup]` section uses (`SignTool=signtool`,
   `SignedUninstaller=yes`, both behind `#ifdef SignInstaller`) to sign
   the installer `.exe` itself and the uninstaller it embeds.

Omitting `-CertificatePath` entirely (the default) skips every part of
this — the exact unsigned build this repo has produced since Sprint 1.
This is the "unsigned fallback for development" requirement: there is no
separate flag to toggle, an unsigned build is just what happens when you
don't pass certificate parameters.

**EV/hardware-token certificates** cannot use `-CertificatePath`/
`-CertificatePassword` as written (there is no exportable `.pfx` file) —
`signtool.exe sign /f ... /p ...` would need to become
`signtool.exe sign /csp ... /kc ...` (or the CA's own signing CLI/cloud
API) referencing the token instead. Revisit this script's signing block
specifically if an EV certificate is the one purchased; the Inno Setup
side (`[Setup] SignTool=`) is unaffected either way, since it only cares
about the resolved `signtool` command line, not how the key is stored.

## 4. CI signing (`.github/workflows/release.yml`)

The release workflow's `Publish and package (installer)` step passes
signing parameters conditionally, based on whether the
`CODE_SIGNING_CERT_BASE64` repository secret is set — see that
workflow's own comments. Required secrets, once a certificate exists:

| Secret name | Contents |
|---|---|
| `CODE_SIGNING_CERT_BASE64` | The `.pfx`/`.p12` file, base64-encoded (`[Convert]::ToBase64String((Get-Content cert.pfx -AsByteStream))` or equivalent) |
| `CODE_SIGNING_CERT_PASSWORD` | The certificate's private-key password |

The workflow decodes the base64 secret to a temp `.pfx` on the runner,
passes its path as `-CertificatePath`, and the temp file is discarded
when the job ends (GitHub-hosted runners are ephemeral — nothing
persists between runs). Until both secrets exist, the workflow's signing
step is skipped entirely and the release artifact is unsigned, matching
the local default exactly.

## 5. Verifying a signature after signing

```powershell
signtool verify /pa "artifacts\ROJAN Reception Setup.exe"
Get-AuthenticodeSignature "artifacts\ROJAN Reception Setup.exe" | Format-List
```

`SignerCertificate` should show the real CA-issued certificate, and
`Status` should read `Valid` (not `NotSigned`/`UnknownError`).
