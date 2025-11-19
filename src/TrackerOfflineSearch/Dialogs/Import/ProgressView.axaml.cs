using Avalonia.Controls;
using TrackerOfflineSearch.Dialogs.ConfirmCancel;

namespace TrackerOfflineSearch.Dialogs.Import;

[ExcludeFromCodeCoverage]
public partial class ProgressView : ReactiveUserControl<ProgressViewModel>
{
    public ProgressView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            vm.ConfirmCancel
                .RegisterHandler(ConfirmCancelAsync)
                .DisposeWith(d);
        });
    }

    private async Task ConfirmCancelAsync(IInteractionContext<Unit, bool> interaction)
    {
        if (TopLevel.GetTopLevel(this) is not Window topLevel)
        {
            interaction.SetOutput(output: false);
            return;
        }

        var dialog = new ConfirmCancelDialog();

        var result = await dialog.ShowDialog<bool>(topLevel).ConfigureAwait(false);

        interaction.SetOutput(output: result);
    }

}
