using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TrackerSearch.ViewModels;

namespace TrackerSearch.Views;

public partial class LoadingView : ReactiveUserControl<LoadingViewModel>
{
    public LoadingView()
    {
        InitializeComponent();
    }
}

