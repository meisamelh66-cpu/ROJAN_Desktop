using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Shell.Organizations;

/// <summary>
/// Phase 2B Context State Hardening: the production <see cref="IDemoModeProvider"/> -
/// reads the <c>ROJAN_DESKTOP_DEMO_MODE</c> environment variable, and only
/// in DEBUG builds (compiled to always <see langword="false"/> in Release,
/// same defense-in-depth posture as ROJAN_Backend's own
/// <c>DevOtpModeGuard</c> refusing to allow its dev-only path when the
/// "prod" profile is active - here expressed as a build-configuration gate
/// since Desktop has no equivalent runtime profile concept). Off by
/// default - no environment this project's sessions have run in has ever
/// set this variable, so every existing deployment is unaffected.
/// </summary>
public sealed class EnvironmentDemoModeProvider : IDemoModeProvider
{
    private const string FlagName = "ROJAN_DESKTOP_DEMO_MODE";

    public bool IsEnabled =>
#if DEBUG
        string.Equals(Environment.GetEnvironmentVariable(FlagName), "true", StringComparison.OrdinalIgnoreCase);
#else
        false;
#endif
}
