using System.Windows;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Shell.Workspaces;

/// <summary>Code-behind for <see cref="FloatingModuleWindow"/> - window-command handlers only, the same "no code-behind except window chrome" exception <c>MainWindow.xaml.cs</c>'s own doc comment documents.</summary>
public partial class FloatingModuleWindow : Window
{
    public FloatingModuleWindow(ModuleDescriptor descriptor, ViewModelBase content)
    {
        InitializeComponent();
        Title = descriptor.Metadata.Title;
        TitleText.Text = descriptor.Metadata.Title;
        ContentHost.Content = content;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
