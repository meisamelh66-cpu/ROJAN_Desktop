using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Rojan.Desktop.Presentation.Localization;

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
    private const int MaxRebuildRetries = 3;

    private int _rebuildRetryCount;

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

        // Phase B-1 fix: complements the bounded Dispatcher retry in
        // Rebuild() - if ActualWidth was still 0 when Rebuild() last ran
        // (so it bailed out with nothing rendered), SizeChanged is the
        // idiomatic WPF signal that a real width is now available.
        // Guarded to "no clock yet" so this never fights an already-
        // running marquee on an ordinary window resize.
        SizeChanged += (_, _) =>
        {
            if (_scrollClock is null)
            {
                Rebuild();
            }
        };
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

    private void Rebuild(bool isRetry = false)
    {
        if (!isRetry)
        {
            _rebuildRetryCount = 0;
        }

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
            // Phase B-1 fix: an out-of-band manual Measure() call like the
            // one above can race a layout pass that hasn't settled yet
            // (observed intermittently - items added to TrackPanel, but
            // DesiredSize still reads 0 this tick), which previously left
            // the ticker silently blank with no retry. Bounded to
            // MaxRebuildRetries (not an open-ended retry loop) - if the
            // width is still 0 after a few attempts, something other than
            // a one-tick layout race is wrong, and retrying forever would
            // risk starving the dispatcher instead of fixing anything.
            if (_rebuildRetryCount < MaxRebuildRetries)
            {
                _rebuildRetryCount++;
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Rebuild(isRetry: true)));
            }

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

        // Phase B-1 (visual refinement): the one reference-parity "LIVE"
        // badge - a small filled pill reusing Rojan.Brush.Error, the same
        // token the Notification Center's unread-count badge already uses
        // for an urgent/live indicator, so this isn't a new color concept.
        if (item.IsLive)
        {
            var badge = new Border
            {
                Background = (Brush)System.Windows.Application.Current.FindResource("Rojan.Brush.Error"),
                CornerRadius = (CornerRadius)System.Windows.Application.Current.FindResource("Rojan.CornerRadius.Pill"),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            badge.Child = new TextBlock
            {
                Text = Strings.News_LiveBadge,
                FontFamily = (FontFamily)System.Windows.Application.Current.FindResource("Rojan.FontFamily.Default"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)System.Windows.Application.Current.FindResource("Rojan.Brush.ButtonText"),
            };
            content.Children.Add(badge);
        }

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
