using Rojan.Desktop.Presentation.Dialogs;

namespace Rojan.Desktop.Presentation.Tests.Dialogs;

/// <summary>Records every dialog shown/closed so ViewModel tests can assert on dialog-host interaction without a real Shell.</summary>
internal sealed class StubDialogService : IDialogService
{
    public List<object> ShownDialogs { get; } = [];

    public int CloseCount { get; private set; }

    public object? ActiveDialog { get; private set; }

    public void ShowDialog(object viewModel)
    {
        ShownDialogs.Add(viewModel);
        ActiveDialog = viewModel;
    }

    public void CloseDialog()
    {
        CloseCount++;
        ActiveDialog = null;
    }
}
