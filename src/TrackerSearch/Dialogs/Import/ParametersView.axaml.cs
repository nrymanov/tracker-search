using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace TrackerSearch.Dialogs.Import;

public partial class ParametersView : ReactiveUserControl<ParametersViewModel>
{
    public ParametersView()
    {
        InitializeComponent();
    }

    private async void OnSelectArchive(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null)
        {
            return;
        }
        
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Сжатый архив форума") { Patterns = ["*.xml.xz"] },
                FilePickerFileTypes.All,
                ],
        }).ConfigureAwait(true);

        if (files.Count == 1)
        {
            vm.ArchivePath = files[0].TryGetLocalPath() ?? "";
        }
    }
}
