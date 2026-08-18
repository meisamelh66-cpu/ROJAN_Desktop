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
    public void IsEnabled_VariableSetToTrue_ReturnsTrueInDebugBuilds()
    {
        // dotnet test always exercises the Debug configuration in this solution (confirmed by every
        // test run's own bin/Debug output path), so the #if DEBUG branch is genuinely under test here,
        // not skipped - this assertion would need updating only if that build-configuration assumption
        // ever changes.
        var original = Environment.GetEnvironmentVariable(FlagName);
        try
        {
            Environment.SetEnvironmentVariable(FlagName, "true");
            var provider = new EnvironmentDemoModeProvider();

            Assert.True(provider.IsEnabled);
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
    public void IsEnabled_VariableSetWithDifferentCasing_StillReturnsTrue()
    {
        var original = Environment.GetEnvironmentVariable(FlagName);
        try
        {
            Environment.SetEnvironmentVariable(FlagName, "TRUE");
            var provider = new EnvironmentDemoModeProvider();

            Assert.True(provider.IsEnabled);
        }
        finally
        {
            Environment.SetEnvironmentVariable(FlagName, original);
        }
    }
}
