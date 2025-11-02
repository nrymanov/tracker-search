using Avalonia.Controls;

namespace TrackerSearch.Dialogs.Import;

public partial class ImportWizard : ReactiveWindow<ImportWizardViewModel>
{
    public ImportWizard()
    {
        InitializeComponent();

        this.WhenActivated(d => {
            var vm = ViewModel;
            if (vm is null)
            {
                return;
            }

            vm.CancelCommand
                .Where(canClose => canClose)
                .Subscribe(_ => Close())
                .DisposeWith(d);
        });
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.IsProgrammatic)
        {
            // Окно закрывается явным вызовом Close() - не мешаем!
            return;
        }

        // Окно закрывается в результате действия пользователя (нажатие на крестик, пункт в системном меню)
        // Дадим шанс проверить возможность отмены и спросить падтверждение от пользователя.
        // Для этого явно запустим команду модели Cancel, а она уже опросит страницы.
        var vm = ViewModel;
        if (vm is null)
        {
            return;
        }

        e.Cancel = true;

        Observable.Return(Unit.Default).InvokeCommand(vm.CancelCommand);
    }
}
