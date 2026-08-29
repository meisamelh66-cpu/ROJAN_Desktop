using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.QrCodes;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Salons;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.QrCodes;

namespace Rojan.Desktop.Presentation.Tests.QrCodes;

public sealed class QrCodesPageViewModelTests
{
    private static SalonDto MakeSalon(string id = "salon-1") =>
        new(id, "Glow Salon", "A nice salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St", true);

    [Fact]
    public async Task Constructor_LoadsManagerCustomerQrAndSalon_StateIsLoaded()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(MakeSalon()))
        {
            GetSalonQrCode = (_, _) => Task.FromResult<byte[]>([9, 9, 9]),
        };
        var sut = new QrCodesPageViewModel(queryService, new StubSalonInviteService(), new StubStaticQrCodeGenerator([1, 2, 3]));

        await Task.Delay(10);

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.Equal("Glow Salon", sut.Salon?.Name);
        Assert.Equal(new byte[] { 1, 2, 3 }, sut.ManagerQrBytes);
        Assert.Equal(new byte[] { 9, 9, 9 }, sut.CustomerQrBytes);
        Assert.Null(sut.ReceptionInviteQrBytes);
        Assert.False(sut.HasReceptionInviteQr);
        Assert.True(sut.IsReadyToPrint);
    }

    [Fact]
    public void Constructor_GeneratesManagerQrForTheRealDownloadUrl()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(MakeSalon())) { GetSalonQrCode = (_, _) => Task.FromResult<byte[]>([1]) };
        var generator = new StubStaticQrCodeGenerator([1]);

        _ = new QrCodesPageViewModel(queryService, new StubSalonInviteService(), generator);

        Assert.Equal(QrCodesPageViewModel.ManagerDownloadUrl, generator.LastUrl);
        Assert.Equal("https://rojanai.ir/download/manager", QrCodesPageViewModel.ManagerDownloadUrl);
    }

    [Fact]
    public async Task Constructor_SalonQueryFails_StateIsError()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromException<SalonDto?>(new InvalidOperationException("boom")));
        var sut = new QrCodesPageViewModel(queryService, new StubSalonInviteService(), new StubStaticQrCodeGenerator([1]));

        await Task.Delay(10);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.False(sut.IsReadyToPrint);
    }

    // Phase 8.23 Logging Wave 2B: LoadAsync / GenerateReceptionInviteAsync now log at
    // Error before their existing handling - user-visible behaviour unchanged.

    [Fact]
    public async Task LoadAsync_QueryThrows_LogsError()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromException<SalonDto?>(new InvalidOperationException("boom")));
        var logger = new RecordingLogger<QrCodesPageViewModel>();
        var sut = new QrCodesPageViewModel(queryService, new StubSalonInviteService(), new StubStaticQrCodeGenerator([1]), logger);

        await Task.Delay(10);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateReceptionInviteCommand_BackendRejects_LogsError()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(MakeSalon())) { GetSalonQrCode = (_, _) => Task.FromResult<byte[]>([1]) };
        var inviteService = new StubSalonInviteService { CreateException = new InvalidOperationException("Forbidden") };
        var logger = new RecordingLogger<QrCodesPageViewModel>();
        var sut = new QrCodesPageViewModel(queryService, inviteService, new StubStaticQrCodeGenerator([1]), logger);
        await Task.Delay(10);

        sut.GenerateReceptionInviteCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal(Strings.Common_ActionFailedMessage, sut.GenerateInviteErrorMessage);
        Assert.DoesNotContain("Forbidden", sut.GenerateInviteErrorMessage ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("GenerateReceptionInviteAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromException<SalonDto?>(new InvalidOperationException("boom")));

        var exception = Record.Exception(() => new QrCodesPageViewModel(queryService, new StubSalonInviteService(), new StubStaticQrCodeGenerator([1])));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GenerateReceptionInviteCommand_CreatesInviteAndFetchesItsQrCode()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(MakeSalon())) { GetSalonQrCode = (_, _) => Task.FromResult<byte[]>([1]) };
        var inviteService = new StubSalonInviteService
        {
            CreateResult = new CreatedInviteDto("invite-1", "tok-abc"),
            QrCodeBytes = [4, 5, 6],
        };
        var sut = new QrCodesPageViewModel(queryService, inviteService, new StubStaticQrCodeGenerator([1]));
        await Task.Delay(10);

        sut.GenerateReceptionInviteCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal(new byte[] { 4, 5, 6 }, sut.ReceptionInviteQrBytes);
        Assert.True(sut.HasReceptionInviteQr);
        Assert.Equal("salon-1", inviteService.LastCreateSalonId);
        Assert.False(sut.HasGenerateInviteError);
    }

    [Fact]
    public async Task GenerateReceptionInviteCommand_BackendRejects_SetsGenerateInviteErrorMessage()
    {
        var queryService = new StubSalonQueryService(_ => Task.FromResult<SalonDto?>(MakeSalon())) { GetSalonQrCode = (_, _) => Task.FromResult<byte[]>([1]) };
        var inviteService = new StubSalonInviteService { CreateException = new InvalidOperationException("Forbidden") };
        var sut = new QrCodesPageViewModel(queryService, inviteService, new StubStaticQrCodeGenerator([1]));
        await Task.Delay(10);

        sut.GenerateReceptionInviteCommand.Execute(null);
        await Task.Delay(10);

        Assert.Null(sut.ReceptionInviteQrBytes);
        Assert.True(sut.HasGenerateInviteError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.GenerateInviteErrorMessage);
    }

    private sealed class StubStaticQrCodeGenerator(byte[] bytes) : IStaticQrCodeGenerator
    {
        public string? LastUrl { get; private set; }

        public byte[] GeneratePng(string url, int sizePx)
        {
            LastUrl = url;
            return bytes;
        }
    }

    private sealed class StubSalonInviteService : ISalonInviteService
    {
        public CreatedInviteDto? CreateResult { get; set; }

        public Exception? CreateException { get; set; }

        public byte[]? QrCodeBytes { get; set; }

        public string? LastCreateSalonId { get; private set; }

        public Task<SalonInviteDetailsDto> GetDetailsAsync(string token, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("QrCodesPageViewModel never looks up an invite by token.");

        public Task<AcceptedMembershipDto> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("QrCodesPageViewModel never accepts invites.");

        public Task<CreatedInviteDto> CreateReceptionInviteAsync(string salonId, CancellationToken cancellationToken = default)
        {
            LastCreateSalonId = salonId;
            return CreateException is not null ? Task.FromException<CreatedInviteDto>(CreateException) : Task.FromResult(CreateResult!);
        }

        public Task<byte[]> GetInviteQrCodeAsync(string salonId, string inviteId, int sizePx, CancellationToken cancellationToken = default) =>
            Task.FromResult(QrCodeBytes!);
    }
}
