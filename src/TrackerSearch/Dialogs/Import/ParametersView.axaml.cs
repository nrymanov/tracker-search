using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace TrackerSearch.Dialogs.Import;

public partial class ParametersView : ReactiveUserControl<ParametersViewModel>
{
    public ParametersView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
            ViewModel!.SelectArchive
                .RegisterHandler(SelectArchiveAsync)
                .DisposeWith(d)
        );
    }

    private async Task SelectArchiveAsync(IInteractionContext<Unit, string> interaction)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Сжатый архив форума") { Patterns = ["*.xml.xz"] },
                FilePickerFileTypes.All,
                ],
        }).ConfigureAwait(false);

        if (files.Count == 1)
        {
            interaction.SetOutput(files[0].TryGetLocalPath() ?? "");
        }
    }
}
