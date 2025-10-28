using System.IO;
using Avalonia.Svg.Skia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TrackerOfflineSearch.Core.Interfaces;
using TrackerOfflineSearch.Core.Services;
using TrackerSearch.Services;
using TrackerSearch.ViewModels;

namespace TrackerSearch;

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
            .Configure<ApplicationsOptions>(configuration.GetSection(nameof(ApplicationsOptions)))
            .AddSingleton(configuration);

        var logger = RegisterLogger(configuration, services);

        logger.Verbose("ConfigureServices - begin");
        logger.Debug("ConfigureServices - begin");
        logger.Information("ConfigureServices - begin");
        logger.Warning("ConfigureServices - begin");
        logger.Error("ConfigureServices - begin");
        logger.Fatal("ConfigureServices - begin");

        services
            .AddSingleton<IPostMapper, PostMapper>()
            .AddSingleton<IBBTextConverter, BBTextConverter>()
            .AddSingleton<IIndexSearchService, LuceneSearchService>()
            .AddTransient<IIndexImportService, LuceneImportService>();

        services
            .AddSingleton<ILoadingViewModel, LoadingViewModel>()
            .AddSingleton<ISearchViewModel, SearchViewModel>()
            .AddSingleton<IImportViewModel, ImportViewModel>()
            .AddSingleton<IAboutViewModel, AboutViewModel>()
            .AddSingleton<MainWindowViewModel>();
        
        return services.BuildServiceProvider();
    }
}
