using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Salons;

namespace Rojan.Desktop.Presentation.Tests.Salons;

public sealed class SalonPageViewModelTests
{
    private static SalonDto MakeSalon(string id = "salon-1", bool active = true) =>
        new(id, "Glow Salon", "A nice salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St", active);

    [Fact]
    public void Constructor_QueryStillLoading_StateIsLoading()
    {
        var tcs = new TaskCompletionSource<SalonDto?>();
        var queryService = new StubSalonQueryService(_ => tcs.Task);

        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService());

        Assert.Equal(DashboardState.Loading, sut.State);
    }

    [Fact]
    public void Constructor_NoSalon_StateIsLoadedButNeedsSalon()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));

        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.False(sut.HasSalon);
        Assert.True(sut.NeedsSalon);
        Assert.Null(sut.Salon);
    }

    [Fact]
    public void Constructor_SalonExists_StateIsLoadedAndHasSalon()
    {
        var salon = MakeSalon();
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(salon));

        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService());

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.True(sut.HasSalon);
        Assert.False(sut.NeedsSalon);
        Assert.Equal(salon, sut.Salon);
    }

    [Fact]
    public void Constructor_QueryThrows_StateIsErrorAndSetsErrorMessage()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromException<SalonDto?>(new InvalidOperationException("boom")));

        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService());

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
    }

    // Phase 8.23 Logging Wave 2B: LoadAsync / CreateSalonAsync now log at Error before
    // their existing handling - user-visible behaviour unchanged.

    [Fact]
    public void LoadAsync_QueryThrows_LogsError()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromException<SalonDto?>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<SalonPageViewModel>();

        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService(), logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateSalonAsync_CommandThrows_LogsError()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));
        var commandService = new StubSalonCommandService((_, _) => Task.FromException<SalonDto>(new InvalidOperationException("save boom")));
        var logger = new RecordingLogger<SalonPageViewModel>();
        var sut = new SalonPageViewModel(queryService, commandService, logger)
        {
            Name = "Glow Salon",
            Phone = "+1 555 0100",
            Address = "1 Main St",
        };
        await Task.Delay(10);

        sut.CreateSalonCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal("save boom", sut.CreateErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("CreateSalonAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromException<SalonDto?>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() => new SalonPageViewModel(queryService, new StubSalonCommandService()));

        Assert.Null(exception);
    }

    [Fact]
    public void LoadCommand_ExecutedAfterFailure_RecoversToLoadedState()
    {
        var shouldFail = true;
        var salon = MakeSalon();
        var queryService = new StubSalonQueryService(_ => shouldFail
            ? Task.FromException<SalonDto?>(new InvalidOperationException("boom"))
            : Task.FromResult<SalonDto?>(salon));
        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService());
        Assert.Equal(DashboardState.Error, sut.State);

        shouldFail = false;
        sut.LoadCommand.Execute(null);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Null(sut.ErrorMessage);
        Assert.True(sut.HasSalon);
    }

    [Fact]
    public void CreateSalonCommand_CanExecute_FalseUntilNamePhoneAndAddressAreFilled()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));
        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService());

        Assert.False(sut.CreateSalonCommand.CanExecute(null));

        sut.Name = "Glow Salon";
        Assert.False(sut.CreateSalonCommand.CanExecute(null));

        sut.Phone = "+1 555 0100";
        Assert.False(sut.CreateSalonCommand.CanExecute(null));

        sut.Address = "1 Main St";
        Assert.True(sut.CreateSalonCommand.CanExecute(null));
    }

    [Fact]
    public void CreateSalonCommand_WhitespaceOnlyRequiredFields_CanExecuteIsFalse()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));
        var sut = new SalonPageViewModel(queryService, new StubSalonCommandService())
        {
            Name = "   ",
            Phone = "+1 555 0100",
            Address = "1 Main St",
        };

        Assert.False(sut.CreateSalonCommand.CanExecute(null));
    }

    [Fact]
    public void CreateSalonCommand_Success_PopulatesSalonAndClearsCreatingState()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));
        var commandService = new StubSalonCommandService();
        var sut = new SalonPageViewModel(queryService, commandService)
        {
            Name = "  Glow Salon  ",
            Description = "  A nice salon.  ",
            Phone = "  +1 555 0100  ",
            Email = "  hello@glowsalon.example  ",
            Address = "  1 Main St  ",
        };

        sut.CreateSalonCommand.Execute(null);

        Assert.True(sut.HasSalon);
        Assert.False(sut.IsCreating);
        Assert.Null(sut.CreateErrorMessage);
        Assert.False(sut.HasCreateError);

        var sent = Assert.Single(commandService.CreateCalls);
        Assert.Equal("Glow Salon", sent.Name);
        Assert.Equal("A nice salon.", sent.Description);
        Assert.Equal("+1 555 0100", sent.Phone);
        Assert.Equal("hello@glowsalon.example", sent.Email);
        Assert.Equal("1 Main St", sent.Address);
    }

    [Fact]
    public void CreateSalonCommand_BlankOptionalFields_SentAsNull()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));
        var commandService = new StubSalonCommandService();
        var sut = new SalonPageViewModel(queryService, commandService)
        {
            Name = "Glow Salon",
            Phone = "+1 555 0100",
            Address = "1 Main St",
        };

        sut.CreateSalonCommand.Execute(null);

        var sent = Assert.Single(commandService.CreateCalls);
        Assert.Null(sent.Description);
        Assert.Null(sent.Email);
    }

    [Fact]
    public void CreateSalonCommand_Failure_SetsCreateErrorMessageAndLeavesFormVisible()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(null));
        var commandService = new StubSalonCommandService((_, _) => Task.FromException<SalonDto>(new InvalidOperationException("Validation failed")));
        var sut = new SalonPageViewModel(queryService, commandService)
        {
            Name = "Glow Salon",
            Phone = "+1 555 0100",
            Address = "1 Main St",
        };

        sut.CreateSalonCommand.Execute(null);

        Assert.False(sut.HasSalon);
        Assert.True(sut.NeedsSalon);
        Assert.False(sut.IsCreating);
        Assert.Equal("Validation failed", sut.CreateErrorMessage);
        Assert.True(sut.HasCreateError);
        // The typed input must survive a failed attempt, so the owner can correct and retry.
        Assert.Equal("Glow Salon", sut.Name);
    }
}
