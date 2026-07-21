using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Rojan.Desktop.Presentation.Controls.Dashboard;

/// <summary>
/// Phase 36: right-to-left infinite marquee - see NewsTicker.xaml for the
/// seamless-loop technique. Rebuilds its two content copies whenever
/// ItemsSource changes (including live collection changes, if the source
/// implements INotifyCollectionChanged - the same lesson the Bug 3 fix
/// learned: a snapshot taken once would miss data that arrives after this
/// control is first bound). Animates via a directly-applied, controllable
/// AnimationClock (ScrollTransform.ApplyAnimationClock + the clock's own
/// Controller.Pause/Resume) rather than Storyboard.Begin(this, true) -
/// the same BeginAnimation-family approach KPIValue's count-up already
/// uses successfully elsewhere in this app; a Storyboard handed a directly
/// object-targeted (non-named) animation did not reliably drive the
/// property here.
/// </summary>
public partial class NewsTicker : UserControl
{
    private const double PixelsPerSecond = 55;

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(NewsTicker),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private AnimationClock? _scrollClock;
    private INotifyCollectionChanged? _subscribedSource;

    public NewsTicker()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (NewsTicker)d;

        if (self._subscribedSource is not null)
        {
            self._subscribedSource.CollectionChanged -= self.OnSourceCollectionChanged;
            self._subscribedSource = null;
        }

        if (e.NewValue is INotifyCollectionChanged notifier)
        {
            notifier.CollectionChanged += self.OnSourceCollectionChanged;
            self._subscribedSource = notifier;
        }

        self.Rebuild();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Root_MouseEnter(object sender, MouseEventArgs e) => _scrollClock?.Controller?.Pause();

    private void Root_MouseLeave(object sender, MouseEventArgs e) => _scrollClock?.Controller?.Resume();

    private void Rebuild()
    {
        if (!IsLoaded || ActualWidth <= 0)
        {
            return;
        }

        ScrollTransform.ApplyAnimationClock(TranslateTransform.XProperty, null);
        _scrollClock = null;
        TrackPanel.Children.Clear();
        ScrollTransform.X = 0;

        var items = (ItemsSource?.Cast<NewsTickerItem>() ?? Enumerable.Empty<NewsTickerItem>()).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var copyOne = BuildRow(items);
        var copyTwo = BuildRow(items);
        TrackPanel.Children.Add(copyOne);
        TrackPanel.Children.Add(copyTwo);

        copyOne.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var copyWidth = copyOne.DesiredSize.Width;
        if (copyWidth <= 0)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            From = 0,
            To = -copyWidth,
            Duration = TimeSpan.FromSeconds(copyWidth / PixelsPerSecond),
            RepeatBehavior = RepeatBehavior.Forever,
        };

        _scrollClock = animation.CreateClock();
        ScrollTransform.ApplyAnimationClock(TranslateTransform.XProperty, _scrollClock);
    }

    private static StackPanel BuildRow(IReadOnlyList<NewsTickerItem> items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };

        foreach (var item in items)
        {
            row.Children.Add(BuildItemButton(item));
            row.Children.Add(BuildSeparator());
        }

        return row;
    }

    private static Button BuildItemButton(NewsTickerItem item)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };

        content.Children.Add(new TextBlock
        {
            Text = item.Icon,
            FontFamily = (FontFamily)System.Windows.Application.Current.FindResource("Rojan.FontFamily.Icons"),
            FontSize = 13,
            Foreground = (Brush)System.Windows.Application.Current.FindResource("Rojan.Brush.Accent"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
        });

        content.Children.Add(new TextBlock
        {
            Text = item.Text,
            FontFamily = (FontFamily)System.Windows.Application.Current.FindResource("Rojan.FontFamily.Default"),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)System.Windows.Application.Current.FindResource("Rojan.Brush.TextPrimary"),
            VerticalAlignment = VerticalAlignment.Center,
        });

        var button = new Button
        {
            Content = content,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Template = BuildTransparentButtonTemplate(),
        };

        // Every ticker item is clickable per spec - items with no real
        // destination still get a "coming soon" action (assigned by
        // DashboardPage), never left inert.
        var onClick = item.OnClick;
        if (onClick is not null)
        {
            button.Click += (_, _) => onClick();
        }

        return button;
    }

    private static ControlTemplate BuildTransparentButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
        presenterFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        template.VisualTree = presenterFactory;
        return template;
    }

    private static TextBlock BuildSeparator() => new()
    {
        Text = "•",
        FontFamily = (FontFamily)System.Windows.Application.Current.FindResource("Rojan.FontFamily.Default"),
        FontSize = 13,
        Foreground = (Brush)System.Windows.Application.Current.FindResource("Rojan.Brush.MutedText"),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(10, 0, 10, 0),
    };
}
