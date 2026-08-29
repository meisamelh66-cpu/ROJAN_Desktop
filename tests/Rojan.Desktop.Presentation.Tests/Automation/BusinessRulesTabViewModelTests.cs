using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class BusinessRulesTabViewModelTests
{
    private const string Secret = "IF-Customer-is-VIP-SECRET";

    private static (BusinessRulesTabViewModel Sut, StubBusinessRuleService Rules) CreateSut()
    {
        var rules = new StubBusinessRuleService();
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1");
        return (sut, rules);
    }

    [Fact]
    public async Task LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var rules = new StubBusinessRuleService { GetAllException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<BusinessRulesTabViewModel>();
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1", logger);

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=LoadAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak()
    {
        var rules = new StubBusinessRuleService { CreateException = new InvalidOperationException(Secret) };
        var logger = new RecordingLogger<BusinessRulesTabViewModel>();
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1", logger);
        sut.NewRuleName = "VIP Discount";
        sut.NewRuleField = "IsVip";

        sut.CreateCommand.Execute(null);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=CreateAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows()
    {
        var rules = new StubBusinessRuleService { GetAllException = new InvalidOperationException("boom") };
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1");

        await sut.LoadAsync();

        Assert.Equal(DashboardState.Error, sut.State);
    }

    [Fact]
    public void LoadCommand_NoRulesYet_StateIsEmpty()
    {
        var (sut, _) = CreateSut();

        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Empty, sut.State);
    }

    [Fact]
    public void CreateCommand_CanExecute_OnlyWhenNameAndFieldAreProvided()
    {
        var (sut, _) = CreateSut();

        Assert.False(sut.CreateCommand.CanExecute(null));

        sut.NewRuleName = "VIP Discount";
        Assert.False(sut.CreateCommand.CanExecute(null));

        sut.NewRuleField = "IsVip";
        Assert.True(sut.CreateCommand.CanExecute(null));
    }

    [Fact]
    public void CreateCommand_AddsANewEnabledRule()
    {
        var (sut, _) = CreateSut();
        sut.NewRuleName = "VIP Discount";
        sut.NewRuleField = "IsVip";
        sut.NewRuleValue = "true";
        sut.NewRuleActionType = BusinessRuleActionType.ApplyDiscount;
        sut.NewRuleActionValue = "10";

        sut.CreateCommand.Execute(null);

        Assert.Single(sut.Rules);
        Assert.True(sut.Rules[0].IsEnabled);
        Assert.Equal("10", sut.Rules[0].Action.Parameters["percentage"]);
        Assert.Equal(string.Empty, sut.NewRuleName);
    }

    [Fact]
    public void ToggleEnabledCommand_FlipsIsEnabled()
    {
        var (sut, _) = CreateSut();
        sut.NewRuleName = "VIP Discount";
        sut.NewRuleField = "IsVip";
        sut.CreateCommand.Execute(null);
        var rule = sut.Rules[0];

        sut.ToggleEnabledCommand.Execute(rule);

        Assert.False(sut.Rules[0].IsEnabled);
    }

    [Fact]
    public void ToggleEnabledCommand_Failure_ShowsGenericError_PreservesRuleState_LogsOperationOnly()
    {
        var rules = new StubBusinessRuleService();
        var logger = new RecordingLogger<BusinessRulesTabViewModel>();
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1", logger);
        sut.NewRuleName = "VIP Discount";
        sut.NewRuleField = "IsVip";
        sut.CreateCommand.Execute(null);
        rules.SetEnabledException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => sut.ToggleEnabledCommand.Execute(sut.Rules[0]));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.True(sut.Rules[0].IsEnabled);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=ToggleEnabledAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteCommand_Failure_ShowsGenericError_PreservesRule_LogsOperationOnly()
    {
        var rules = new StubBusinessRuleService();
        var logger = new RecordingLogger<BusinessRulesTabViewModel>();
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1", logger);
        sut.NewRuleName = "VIP Discount";
        sut.NewRuleField = "IsVip";
        sut.CreateCommand.Execute(null);
        rules.DeleteException = new InvalidOperationException(Secret);

        var exception = Record.Exception(() => sut.DeleteCommand.Execute(sut.Rules[0]));

        Assert.Null(exception);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.Single(sut.Rules);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("Operation=DeleteAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteCommand_RemovesTheRule()
    {
        var (sut, _) = CreateSut();
        sut.NewRuleName = "VIP Discount";
        sut.NewRuleField = "IsVip";
        sut.CreateCommand.Execute(null);
        var rule = sut.Rules[0];

        sut.DeleteCommand.Execute(rule);

        Assert.Empty(sut.Rules);
    }
}
