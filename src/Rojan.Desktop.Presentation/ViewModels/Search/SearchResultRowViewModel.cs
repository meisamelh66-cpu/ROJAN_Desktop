using System.Windows.Input;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Search;

/// <summary>One row in the Command Palette's results/favorites list - wraps a <see cref="SearchResultDto"/> with its title highlight spans and a per-row Favorite-toggle command.</summary>
public sealed class SearchResultRowViewModel : ViewModelBase
{
    private bool _isFavorite;

    public SearchResultRowViewModel(SearchResultDto result, Func<SearchResultRowViewModel, Task> onToggleFavorite)
    {
        Id = result.Id;
        Type = result.Type;
        Title = result.Title;
        Subtitle = result.Subtitle;
        ActionKey = result.ActionKey;
        TitleHighlights = result.TitleHighlights;
        _isFavorite = result.IsFavorite;

        ToggleFavoriteCommand = new AsyncRelayCommand(_ => onToggleFavorite(this));
    }

    public string Id { get; }

    public SearchResultType Type { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public string ActionKey { get; }

    public IReadOnlyList<HighlightSpan> TitleHighlights { get; }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    public ICommand ToggleFavoriteCommand { get; }
}
