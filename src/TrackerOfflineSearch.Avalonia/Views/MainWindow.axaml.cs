using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;

namespace TrackerOfflineSearch.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        this
            .WhenAnyValue(v => v.PinSplitPane.IsChecked)
            .Subscribe(pin =>
            {
                if (pin ?? false)
                {
                    this.TrackerSplitView.DisplayMode = SplitViewDisplayMode.Inline;
                    this.TrackerSplitView.IsPaneOpen = true;
                }
                else
                {
                    this.TrackerSplitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
                    //this.splitView.IsPaneOpen = false;
                }
            });

        this
            .WhenAnyValue(v => v.TrackerSplitView.IsPointerOver)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isOver =>
            {
                if (this.TrackerSplitView.DisplayMode == SplitViewDisplayMode.Inline)
                    return;

                this.TrackerSplitView.IsPaneOpen = isOver;
            });

        //this
        //    .WhenAnyValue(v => v.htmlText.Text)
        //    .Subscribe(this.webView.LoadHtml);
    }

    private void SplitView_PaneClosing(object? sender, CancelRoutedEventArgs e)
    {
        if (this.TrackerSplitView.DisplayMode == SplitViewDisplayMode.Inline)
        {
            e.Cancel = true;
        }
    }
}
