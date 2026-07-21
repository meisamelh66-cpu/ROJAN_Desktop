using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.Automation;

public sealed class BusinessRulesTabViewModelTests
{
    private static (BusinessRulesTabViewModel Sut, StubBusinessRuleService Rules) CreateSut()
    {
        var rules = new StubBusinessRuleService();
        var sut = new BusinessRulesTabViewModel(rules, "org-1", "branch-1");
        return (sut, rules);
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
