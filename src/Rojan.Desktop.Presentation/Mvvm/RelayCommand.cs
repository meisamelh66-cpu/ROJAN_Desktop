using System.Windows.Input;

namespace Rojan.Desktop.Presentation.Mvvm;

/// <summary>
/// Synchronous <see cref="ICommand"/> that delegates to plain
/// <see cref="Action"/>/<see cref="Func{Object,Boolean}"/> callbacks, hooked
/// into WPF's <see cref="CommandManager.RequerySuggested"/> so
/// <c>CanExecute</c> re-evaluates automatically on the usual UI triggers.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
