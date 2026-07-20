using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Presentation.Help;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.ViewModels.Help;

namespace Rojan.Desktop.Presentation.Tests.Help;

/// <summary>Exercises <see cref="HelpDialogViewModel"/>'s context resolution, back/forward navigation, favorite toggling, and search activation.</summary>
public sealed class HelpDialogViewModelTests
{
    private static HelpTopicDto Topic(string id, string moduleId, IReadOnlyList<string>? relatedTopicIds = null) =>
        new(id, moduleId, null, $"Help_{Capitalize(moduleId)}", [], relatedTopicIds ?? []);

    private static string Capitalize(string value) => char.ToUpperInvariant(value[0]) + value[1..];

    private static HelpDialogViewModel CreateViewModel(IEnumerable<HelpTopicDto> topics, StubDialogService? dialogService = null) =>
        new(
            new StubHelpQueryService(topics),
            new HelpContentResolver(),
            new HelpSearchService(),
            new StubHelpFavoritesStore(),
            new StubHelpRecentlyViewedStore(),
            dialogService ?? new StubDialogService());

    [Fact]
    public async Task InitializeAsync_ResolvesTheTopicForTheGivenModule()
    {
        var viewModel = CreateViewModel([Topic("help-customers", "customers")]);

        await viewModel.InitializeAsync("customers");

        Assert.Equal("help-customers", viewModel.CurrentContent?.TopicId);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task InitializeAsync_PopulatesRelatedTopics()
    {
        var viewModel = CreateViewModel([
            Topic("help-customers", "customers", ["help-bookings"]),
            Topic("help-bookings", "bookings"),
        ]);

        await viewModel.InitializeAsync("customers");

        var related = Assert.Single(viewModel.RelatedTopics);
        Assert.Equal("help-bookings", related.TopicId);
    }

    [Fact]
    public async Task NavigateToTopicCommand_MovesToTheNewTopicAndEnablesBack()
    {
        var viewModel = CreateViewModel([
            Topic("help-customers", "customers", ["help-bookings"]),
            Topic("help-bookings", "bookings"),
        ]);
        await viewModel.InitializeAsync("customers");

        viewModel.NavigateToTopicCommand.Execute("help-bookings");
        await Task.Delay(10);

        Assert.Equal("help-bookings", viewModel.CurrentContent?.TopicId);
        Assert.True(viewModel.CanGoBack);
        Assert.False(viewModel.CanGoForward);
    }

    [Fact]
    public async Task BackCommand_ReturnsToThePreviousTopicAndEnablesForward()
    {
        var viewModel = CreateViewModel([
            Topic("help-customers", "customers", ["help-bookings"]),
            Topic("help-bookings", "bookings"),
        ]);
        await viewModel.InitializeAsync("customers");
        viewModel.NavigateToTopicCommand.Execute("help-bookings");
        await Task.Delay(10);

        viewModel.BackCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal("help-customers", viewModel.CurrentContent?.TopicId);
        Assert.False(viewModel.CanGoBack);
        Assert.True(viewModel.CanGoForward);
    }

    [Fact]
    public async Task ForwardCommand_AfterBack_ReturnsToTheLaterTopic()
    {
        var viewModel = CreateViewModel([
            Topic("help-customers", "customers", ["help-bookings"]),
            Topic("help-bookings", "bookings"),
        ]);
        await viewModel.InitializeAsync("customers");
        viewModel.NavigateToTopicCommand.Execute("help-bookings");
        await Task.Delay(10);
        viewModel.BackCommand.Execute(null);
        await Task.Delay(10);

        viewModel.ForwardCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal("help-bookings", viewModel.CurrentContent?.TopicId);
    }

    [Fact]
    public async Task ToggleFavoriteCommand_TogglesIsFavorite()
    {
        var viewModel = CreateViewModel([Topic("help-customers", "customers")]);
        await viewModel.InitializeAsync("customers");
        Assert.False(viewModel.IsFavorite);

        viewModel.ToggleFavoriteCommand.Execute(null);
        await Task.Delay(10);
        Assert.True(viewModel.IsFavorite);

        viewModel.ToggleFavoriteCommand.Execute(null);
        await Task.Delay(10);
        Assert.False(viewModel.IsFavorite);
    }

    [Fact]
    public async Task SearchText_NonEmpty_ActivatesSearchMode()
    {
        var viewModel = CreateViewModel([Topic("help-customers", "customers")]);
        await viewModel.InitializeAsync("customers");

        viewModel.SearchText = "customers";
        await Task.Delay(10);

        Assert.True(viewModel.IsSearchActive);
    }

    [Fact]
    public async Task SearchText_ClearedAgain_DeactivatesSearchMode()
    {
        var viewModel = CreateViewModel([Topic("help-customers", "customers")]);
        await viewModel.InitializeAsync("customers");
        viewModel.SearchText = "customers";
        await Task.Delay(10);

        viewModel.SearchText = string.Empty;
        await Task.Delay(10);

        Assert.False(viewModel.IsSearchActive);
    }

    [Fact]
    public async Task CloseCommand_ClosesTheDialogViaTheDialogService()
    {
        var dialogService = new StubDialogService();
        var viewModel = CreateViewModel([Topic("help-customers", "customers")], dialogService);
        await viewModel.InitializeAsync("customers");

        viewModel.CloseCommand.Execute(null);

        Assert.Equal(1, dialogService.CloseCount);
    }
}
