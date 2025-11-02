using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace TrackerSearch.Dialogs.Import;

public partial class ProgressView : ReactiveUserControl<ProgressViewModel>
{
    public ProgressView()
    {
        InitializeComponent();
    }
}
