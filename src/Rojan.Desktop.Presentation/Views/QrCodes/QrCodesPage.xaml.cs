using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.ViewModels.QrCodes;

namespace Rojan.Desktop.Presentation.Views.QrCodes;

/// <summary>
/// QR Ecosystem (Desktop Productionization Sprint 1). Unlike every other
/// page in this app, this one has real code-behind logic - printing needs
/// WPF's <see cref="PrintDialog"/>/<see cref="FixedDocument"/> APIs, which
/// have no place on <see cref="QrCodesPageViewModel"/> (keeping it WPF-free
/// and unit-testable is the point - see that class's own doc comment).
/// The print document is built from scratch with hardcoded, print-appropriate
/// styling (black text on white, no theme resources) rather than reusing
/// the app's on-screen Rojan.* styles - a printed page must look the same
/// regardless of the app's current theme/dark-mode setting, and the two
/// concerns (screen theme vs. print output) have no reason to stay coupled.
/// </summary>
public partial class QrCodesPage : UserControl
{
    private const double A4WidthPx = 793.92;
    private const double A4HeightPx = 1122.24;
    private const double MarginPx = 56;

    public QrCodesPage()
    {
        InitializeComponent();
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not QrCodesPageViewModel viewModel || !viewModel.IsReadyToPrint)
        {
            return;
        }

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
        {
            return;
        }

        var document = BuildPrintDocument(viewModel);
        printDialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, Strings.QrCodes_Title);
    }

    private static FixedDocument BuildPrintDocument(QrCodesPageViewModel viewModel)
    {
        var document = new FixedDocument();
        var page = new FixedPage
        {
            Width = A4WidthPx,
            Height = A4HeightPx,
            Background = Brushes.White,
        };

        var root = new StackPanel
        {
            Margin = new Thickness(MarginPx),
            Width = A4WidthPx - (2 * MarginPx),
        };

        root.Children.Add(new TextBlock
        {
            Text = viewModel.Salon!.Name,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            TextAlignment = TextAlignment.Center,
        });

        var contactLine = string.Join("  ·  ", new[] { viewModel.Salon.Address, viewModel.Salon.Phone }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(contactLine))
        {
            root.Children.Add(new TextBlock
            {
                Text = contactLine,
                FontSize = 14,
                Foreground = Brushes.DimGray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var cardsPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 32, 0, 0),
        };

        cardsPanel.Children.Add(BuildQrCard(Strings.QrCodes_ManagerCardTitle, viewModel.ManagerQrBytes!));
        cardsPanel.Children.Add(BuildQrCard(Strings.QrCodes_CustomerCardTitle, viewModel.CustomerQrBytes!));
        if (viewModel.ReceptionInviteQrBytes is not null)
        {
            cardsPanel.Children.Add(BuildQrCard(Strings.QrCodes_ReceptionCardTitle, viewModel.ReceptionInviteQrBytes));
        }

        root.Children.Add(cardsPanel);

        page.Children.Add(root);

        var pageContent = new PageContent();
        ((IAddChild)pageContent).AddChild(page);
        document.Pages.Add(pageContent);

        return document;
    }

    private static StackPanel BuildQrCard(string title, byte[] pngBytes)
    {
        var card = new StackPanel
        {
            Margin = new Thickness(16),
            Width = 200,
        };

        card.Children.Add(new Image
        {
            Source = ToBitmapImage(pngBytes),
            Width = 180,
            Height = 180,
            Stretch = Stretch.Uniform,
        });

        card.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            Foreground = Brushes.Black,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        });

        return card;
    }

    private static BitmapImage ToBitmapImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
