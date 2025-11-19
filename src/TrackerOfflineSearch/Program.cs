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

[ExcludeFromCodeCoverage]
internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        CreateLogger();
        var logger = Log.ForContext<Program>();

        try
        {
            var serviceProvider = ConfigureServices();

            BuildAvaloniaApp()
                .AfterSetup(_ => App.Services = serviceProvider)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception err)
        {
            logger.Fatal(err, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
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

    private static void CreateLogger()
    {
        var logPath = Path.Combine(
            GetApplicationPath(AppConsts.LogsDir),
            "log-.txt"
            );

        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Warning()
#endif
            .MinimumLevel.Override("Avalonia", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("ReactiveUI", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Debug()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:u} {Level:u5} {SourceContext} {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var configuration = GetConfiguration();
        services
            .AddSingleton(configuration)
            .AddSingleton<IConfiguration>(sp => sp.GetRequiredService<IConfigurationRoot>());

        services
            .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

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
                    options.IndexPath = GetApplicationPath(AppConsts.IndexDir);
                }

                //
                // RAMBufferSizeMB должен быть в пределах от 1 до 1024
                //
                if (options.RAMBufferSizeMB <= 0 || 1024 < options.RAMBufferSizeMB)
                {
                    options.RAMBufferSizeMB = AppConsts.RAMBufferSizeMB;
                }
            })
            .ValidateOnStart();

        services
            .AddSingleton<IBackgroundRunner, TaskBackgroundRunner>()
            .AddSingleton<IXmlStreamFactory, XZXmlStreamFactory>()
            .AddSingleton<IArchiveReader, ArchiveReader>()
            .AddSingleton<IBBTextConverter, BBTextConverter>()
            ;

        services
            .AddSingleton<Lucene.Net.Analysis.Analyzer>(
                _ => new Lucene.Net.Analysis.Ru.RussianAnalyzer(AppConsts.SearchEngineVersion)
            )
            .AddSingleton<Lucene.Net.Store.Directory>(DirectoryFactory.GetDirectory)
            .AddSingleton<IIndexService, LuceneIndexService>()
            .AddTransient<IIndexWriterSession, IndexWriterSession>()
            .AddSingleton<Func<IIndexWriterSession>>(sp => sp.GetRequiredService<IIndexWriterSession>)
            ;

        services
            .AddSingleton<MainWindowViewModel>()
            .AddTransient<AboutViewModel>()
            .AddTransient<ImportWizardViewModel>();

        services
            .AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }

    private static string GetApplicationPath(string directoryName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            typeof(Program).Assembly.GetName().Name ?? AppConsts.ApplicationName,
            directoryName
            );
    }
}
