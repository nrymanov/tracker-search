using System.IO;
using Avalonia.Svg.Skia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TrackerOfflineSearch.Dialogs.About;
using TrackerOfflineSearch.Dialogs.Import;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Implementation;
using TrackerOfflineSearch.ViewModels;
using TrackerOfflineSearch.Views;

namespace TrackerOfflineSearch;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var serviceProvider = ConfigureServices();
        BuildAvaloniaApp()
            .AfterSetup(_ => App.Services = serviceProvider)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            ;
    }

    private static IConfigurationRoot GetConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("settings.json", optional: true)
            .Build();

    private static Serilog.ILogger RegisterLogger(IConfiguration configuration, IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

        return Log.ForContext<App>();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var configuration = GetConfiguration();
        services
            .AddSingleton(configuration)
            .AddSingleton<IConfiguration>(sp => sp.GetRequiredService<IConfigurationRoot>());

        services
            .AddOptions<ApplicationsOptions>()
            .Bind(configuration.GetSection(nameof(ApplicationsOptions)))
            .PostConfigure(options =>
            {
                //
                // По умолчанию %LOCALAPPDATA%\TrackerOfflineSearch\Index
                //
                if (string.IsNullOrEmpty(options.IndexPath))
                {
                    options.IndexPath = GetDefaultIndexPath();
                }

                //
                // RAMBufferSizeMB должен быть в пределах от 1 до 1024
                //
                if (options.RAMBufferSizeMB <= 0 || 1024 < options.RAMBufferSizeMB)
                {
                    options.RAMBufferSizeMB = AppConsts.RAMBufferSizeMB;
                }
            })
            .ValidateOnStart()
            ;

        var logger = RegisterLogger(configuration, services);

        logger.Verbose("ConfigureServices - begin");

        services
            .AddSingleton<IArchiveReader, ArchiveReader>()
            .AddSingleton<IBBTextConverter, BBTextConverter>()
            .AddSingleton<IIndexService, LuceneIndexService>()
            ;

        services
            .AddSingleton<MainWindowViewModel>()
            .AddTransient<AboutViewModel>()
            .AddTransient<ImportWizardViewModel>()
            ;

        services
            .AddSingleton<MainWindow>()
            ;

        return services.BuildServiceProvider();
    }

    private static string GetDefaultIndexPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            typeof(Program).Assembly.GetName().Name ?? AppConsts.ApplicationName,
            AppConsts.IndexDir
            );
    }
}
