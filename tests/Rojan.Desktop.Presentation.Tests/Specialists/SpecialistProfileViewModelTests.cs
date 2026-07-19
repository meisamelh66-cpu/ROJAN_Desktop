using Rojan.Desktop.Application.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Specialists;

namespace Rojan.Desktop.Presentation.Tests.Specialists;

public sealed class SpecialistProfileViewModelTests
{
    private static SpecialistProfileDto MakeProfile(string specialistId = "specialist-1") =>
        new(
            new SpecialistDto(specialistId, "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example", "555-0100", SpecialistStatus.Active, "Specializes in balayage."),
            [new SpecialistSkillDto("skill-1", specialistId, "Colour")]);

    [Fact]
    public void Constructor_ProfileQueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<SpecialistProfileDto>();
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => tcs.Task);
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_ProfileQueryReturnsProfile_PopulatesSpecialistAndSkills()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Jordan Lee", sut.Specialist?.FullName);
        Assert.Equal(SpecialistStatus.Active, sut.EditableStatus);
        Assert.Single(sut.Skills);
    }

    [Fact]
    public void Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromException<SpecialistProfileDto>(new InvalidOperationException("boom")));
        var commandService = new StubSpecialistCommandService();

        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    [Fact]
    public void AddSkillCommand_TextIsEmpty_CanExecuteIsFalse()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService);

        Assert.False(sut.AddSkillCommand.CanExecute(null));

        sut.NewSkillText = "Massage";

        Assert.True(sut.AddSkillCommand.CanExecute(null));
    }

    [Fact]
    public void AddSkillCommand_Executed_CallsCommandServiceWithSpecialistIdAndNameThenClearsInput()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService)
        {
            NewSkillText = "Massage",
        };

        sut.AddSkillCommand.Execute(null);

        var call = Assert.Single(commandService.AddSkillCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Equal("Massage", call.Name);
        Assert.Equal(string.Empty, sut.NewSkillText);
    }

    [Fact]
    public void RemoveSkillCommand_Executed_CallsCommandServiceWithSpecialistIdAndSkillId()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService);
        var skill = new SpecialistSkillDto("skill-1", "specialist-1", "Colour");

        sut.RemoveSkillCommand.Execute(skill);

        var call = Assert.Single(commandService.RemoveSkillCalls);
        Assert.Equal("specialist-1", call.SpecialistId);
        Assert.Equal("skill-1", call.SkillId);
    }

    [Fact]
    public void SaveChangesCommand_Executed_CallsUpdateSpecialistAsyncWithEditableStatus()
    {
        var profileQuery = new StubSpecialistProfileQueryService((_, _) => Task.FromResult(MakeProfile()));
        var commandService = new StubSpecialistCommandService();
        var sut = new SpecialistProfileViewModel("specialist-1", profileQuery, commandService)
        {
            EditableStatus = SpecialistStatus.OnLeave,
        };

        sut.SaveChangesCommand.Execute(null);

        var request = Assert.Single(commandService.UpdateRequests);
        Assert.Equal("specialist-1", request.Id);
        Assert.Equal(SpecialistStatus.OnLeave, request.Status);
    }
}
