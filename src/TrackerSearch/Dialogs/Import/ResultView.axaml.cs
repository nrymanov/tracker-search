using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TrackerSearch.Dialogs.Import;

public partial class ResultView : ReactiveUserControl<ResultViewModel>
{
    public ResultView()
    {
        InitializeComponent();
    }
}
