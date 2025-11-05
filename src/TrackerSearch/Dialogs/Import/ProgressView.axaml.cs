using Avalonia.Controls;
using TrackerSearch.Dialogs.ConfirmCancel;

namespace TrackerSearch.Dialogs.Import;

public partial class ProgressView : ReactiveUserControl<ProgressViewModel>
{
    public ProgressView()
    {
        InitializeComponent();

        this.WhenActivated(d => 
            ViewModel!.ConfirmCancel
                .RegisterHandler(HandleImportAsync)
                .DisposeWith(d)
        );
    }

    private async Task HandleImportAsync(IInteractionContext<Unit, bool> interaction)
    {
        var topLevel = TopLevel.GetTopLevel(this) as Window;
        if (topLevel is null)
        {
            interaction.SetOutput(output: false);
            return;
        }

        var dialog = new ConfirmCancelDialog();

        var result = await dialog.ShowDialog<bool>(topLevel).ConfigureAwait(false);

        interaction.SetOutput(output: result);
    }

}
