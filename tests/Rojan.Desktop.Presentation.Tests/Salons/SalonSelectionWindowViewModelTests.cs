using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.Salons;

namespace Rojan.Desktop.Presentation.Tests.Salons;

/// <summary>
/// Exercises <see cref="SalonSelectionWindowViewModel"/> - PASS D6 (Active Salon Context
/// Correctness)'s compact selection surface, shown only when a signed-in account has more than one
/// real candidate salon and none is already active. Same "constructed via new by its opener, no DI"
/// shape as <c>MainWindowViewModel</c>'s Help/Command-Palette dialogs - the candidate list is a
/// real, runtime-only value no test double needs to fabricate beyond the fixture below.
/// </summary>
public sealed class SalonSelectionWindowViewModelTests
{
    private static readonly SalonContext SalonOne = new("salon-1", "Glow Salon", IsOwner: true, MembershipRole: null, Permissions: new HashSet<string>());
    private static readonly SalonContext SalonTwo = new("salon-2", "Shine Salon", IsOwner: true, MembershipRole: null, Permissions: new HashSet<string>());

    [Fact]
    public void Constructor_ExposesTheRealCandidateListUnchanged()
    {
        var sut = new SalonSelectionWindowViewModel(new StubSalonContextService(), [SalonOne, SalonTwo]);

        Assert.Equal([SalonOne, SalonTwo], sut.Candidates);
    }

    [Fact]
    public async Task SelectCommand_RealValidatedCandidate_RaisesSelectedAndClearsErrorMessage()
    {
        var salonContextService = new StubSalonContextService { SelectSalonResult = true };
        var sut = new SalonSelectionWindowViewModel(salonContextService, [SalonOne, SalonTwo]);
        var selectedRaised = false;
        sut.Selected += (_, _) => selectedRaised = true;

        await ExecuteAsync(sut.SelectCommand, SalonTwo, sut);

        Assert.True(selectedRaised);
        Assert.Equal("salon-2", salonContextService.LastSelectedSalonId);
        Assert.Null(sut.ErrorMessage);
    }

    /// <summary>PASS D6 (Rule 8): never trusts the candidate blindly - re-validates through <see cref="ISalonContextService.SelectSalonAsync"/>, and a real rejection (e.g. the account lost access between resolving the list and clicking it) shows an honest message, never a raw error, and never raises <see cref="SalonSelectionWindowViewModel.Selected"/>.</summary>
    [Fact]
    public async Task SelectCommand_ServiceRejectsTheSelection_ShowsHonestErrorAndNeverRaisesSelected()
    {
        var salonContextService = new StubSalonContextService { SelectSalonResult = false };
        var sut = new SalonSelectionWindowViewModel(salonContextService, [SalonOne, SalonTwo]);
        var selectedRaised = false;
        sut.Selected += (_, _) => selectedRaised = true;

        await ExecuteAsync(sut.SelectCommand, SalonOne, sut);

        Assert.False(selectedRaised);
        Assert.Equal(Strings.SalonSelection_Error_InvalidSelection, sut.ErrorMessage);
    }

    /// <summary>AsyncRelayCommand.Execute is "async void" (ICommand's contract) - awaiting the underlying task directly is not possible from here, so this drives it through the command and polls IsBusy back to false, same technique <c>MobileOtpLoginViewModelTests.ExecuteAsync</c> already establishes.</summary>
    private static async Task ExecuteAsync(System.Windows.Input.ICommand command, object parameter, SalonSelectionWindowViewModel viewModel)
    {
        command.Execute(parameter);
        for (var i = 0; i < 100 && viewModel.IsBusy; i++)
        {
            await Task.Delay(10);
        }
    }

    private sealed class StubSalonContextService : ISalonContextService
    {
        public bool SelectSalonResult { get; set; }

        public string? LastSelectedSalonId { get; private set; }

        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task<bool> SelectSalonAsync(string salonId, CancellationToken cancellationToken = default)
        {
            LastSelectedSalonId = salonId;
            return Task.FromResult(SelectSalonResult);
        }
    }
}
