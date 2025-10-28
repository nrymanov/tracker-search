using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TrackerOfflineSearch.Avalonia.ViewModels;
using TrackerOfflineSearch.Avalonia.Views;

namespace TrackerOfflineSearch.Avalonia;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        this.Services = ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = this.Services!.GetRequiredService<MainWindow>();
            mainWindow.DataContext = this.Services!.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public IServiceProvider? Services { get; private set; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services
            .AddSingleton<MainWindow>();

        services
            .AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

}