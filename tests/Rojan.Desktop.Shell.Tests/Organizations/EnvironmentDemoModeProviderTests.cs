using Rojan.Desktop.Shell.Organizations;

namespace Rojan.Desktop.Shell.Tests.Organizations;

/// <summary>
/// Phase 2B Context State Hardening: exercises the real, non-stubbed
/// <see cref="EnvironmentDemoModeProvider"/> - direct coverage for the
/// actual environment-variable-reading production code, distinct from the
/// interface-contract coverage <c>CurrentSessionServiceTests</c> gets via
/// its own stub. Mutates the real process environment variable, so every
/// test restores it in a <c>finally</c> block to avoid leaking state into
/// any other test running in this process.
///
/// CI test-suite fix: the two "variable set to an enabling value" tests
/// below assert against <c>#if DEBUG</c>/<c>#else</c> inside the test body
/// itself, matching <see cref="EnvironmentDemoModeProvider.IsEnabled"/>'s
/// own build-configuration gate exactly - a prior version of this file
/// assumed <c>dotnet test</c> always runs Debug, which is false whenever
/// <c>--configuration Release</c> is passed explicitly (as this repo's own
/// CI release pipeline does), and made the whole solution's Release CI
/// run fail on a false assumption rather than a real defect. This is the
/// intended fix for that: each test now verifies the *correct* behavior
/// for whichever configuration it is actually compiled under - the enable
/// path genuinely exercised in Debug, the safety gate genuinely exercised
/// in Release - rather than a single hardcoded expectation that was only
/// ever true in one of the two.
/// </summary>
public sealed class EnvironmentDemoModeProviderTests
{
    private const string FlagName = "ROJAN_DESKTOP_DEMO_MODE";

    [Fact]
    public void IsEnabled_VariableNotSet_ReturnsFalse()
    {
        var original = Environment.GetEnvironmentVariable(FlagName);
        try
        {
            Environment.SetEnvironmentVariable(FlagName, null);
            var provider = new EnvironmentDemoModeProvider();

            Assert.False(provider.IsEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FlagName, original);
        }
    }

    [Fact]
    public void IsEnabled_VariableSetToTrue_EnablesInDebugAndStaysSafeInRelease()
    {
        // Matches EnvironmentDemoModeProvider.IsEnabled's own #if DEBUG gate exactly - Debug
        // builds genuinely exercise the enable path here, Release builds genuinely exercise the
        // always-false safety gate, so this test verifies real behavior under whichever
        // configuration actually compiled it, not a single assumed-always-true expectation.
        var original = Environment.GetEnvironmentVariable(FlagName);
        try
        {
            Environment.SetEnvironmentVariable(FlagName, "true");
            var provider = new EnvironmentDemoModeProvider();

#if DEBUG
            Assert.True(provider.IsEnabled);
#else
            Assert.False(provider.IsEnabled);
#endif
        }
        finally
        {
            Environment.SetEnvironmentVariable(FlagName, original);
        }
    }

    [Fact]
    public void IsEnabled_VariableSetToSomethingElse_ReturnsFalse()
    {
        var original = Environment.GetEnvironmentVariable(FlagName);
        try
        {
            Environment.SetEnvironmentVariable(FlagName, "1");
            var provider = new EnvironmentDemoModeProvider();

            Assert.False(provider.IsEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FlagName, original);
        }
    }

    [Fact]
    public void IsEnabled_VariableSetWithDifferentCasing_CaseInsensitiveInDebugAndStaysSafeInRelease()
    {
        // Same configuration-aware reasoning as IsEnabled_VariableSetToTrue_EnablesInDebugAndStaysSafeInRelease
        // above - this one additionally covers the case-insensitive comparison itself (Debug only;
        // Release never reaches the comparison at all, so casing is moot there).
        var original = Environment.GetEnvironmentVariable(FlagName);
        try
        {
            Environment.SetEnvironmentVariable(FlagName, "TRUE");
            var provider = new EnvironmentDemoModeProvider();

#if DEBUG
            Assert.True(provider.IsEnabled);
#else
            Assert.False(provider.IsEnabled);
#endif
        }
        finally
        {
            Environment.SetEnvironmentVariable(FlagName, original);
        }
    }
}
