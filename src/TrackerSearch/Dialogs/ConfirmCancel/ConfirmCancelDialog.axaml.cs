using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TrackerSearch.Dialogs.ConfirmCancel;

public partial class ConfirmCancelDialog : Window
{
    public ConfirmCancelDialog()
    {
        InitializeComponent();
    }

    private void ConfirmClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(dialogResult: true);

    private void CancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(dialogResult: false);
}
