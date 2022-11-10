using System.Windows;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using TrackerOfflineSearch.Helpers;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Implementation;
using TrackerOfflineSearch.ViewModels;
using TrackerOfflineSearch.Views;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace TrackerOfflineSearch;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    protected override IContainerExtension CreateContainerExtension()
    {
        var serviceCollection = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        serviceCollection
            .AddSingleton<IConfiguration>(config)
            .AddLogging(loggingBuilder => {
                loggingBuilder
                    .AddConfiguration(config.GetSection("Logging"))
                    .AddDebug();
            });

        var container = new UnityContainer();
        container.BuildServiceProvider(serviceCollection);

        return new UnityContainerExtension(container);
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry
            .RegisterSingleton<IFileSystem, FileSystem>()
            .RegisterSingleton<Analyzer>(() => new StandardAnalyzer(AppConst.SearchEngineVersion))
            .RegisterSingleton<IPostMapper, PostMapper>()
            .RegisterSingleton<IQueryBuilder, QueryBuilder>()
            .RegisterSingleton<IPostRepository, PostRepository>()
            ;

        //containerRegistry
        //    .RegisterForNavigation<HistoryView, HistoryViewModel>();

        //containerRegistry
        //    .RegisterSingleton<SearchViewModel>();
    }

    protected override Window CreateShell()
    {
        var w = Container.Resolve<MainWindow>();
        return w;
    }

    protected override void InitializeShell(Window shell)
    {
        Lucene.Net.Util.InfoStream.Default = this.Container.Resolve<DebugInfoStream>();

        var regionManager = this.Container.Resolve<IRegionManager>();
        regionManager
            // Main views
            .RegisterViewWithRegion(RegionNames.SearchFormRegion, typeof(SearchView))
            .RegisterViewWithRegion(RegionNames.SearchResultRegion, typeof(SearchResultView))
            .RegisterViewWithRegion(RegionNames.SearchToolsRegion, typeof(SearchToolsView))
            // Tools view (inside SearchToolsRegion)
            .RegisterViewWithRegion(RegionNames.HistoryRegion, typeof(HistoryView))
            .RegisterViewWithRegion(RegionNames.ToolsRegion, typeof(ToolsView))
            .RegisterViewWithRegion(RegionNames.InfoRegion, typeof(InfoView))
            ;

        base.InitializeShell(shell);
    }
}
