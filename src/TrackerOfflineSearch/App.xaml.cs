using System.Windows;
using System.Windows.Controls.Ribbon;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using TrackerOfflineSearch.ForumSelector;
using TrackerOfflineSearch.Helpers;
using TrackerOfflineSearch.Services;
using TrackerOfflineSearch.Services.Implementation;
using TrackerOfflineSearch.Settings;
using TrackerOfflineSearch.UpdateWizard.ViewModels;
using TrackerOfflineSearch.UpdateWizard.Views;
using TrackerOfflineSearch.Views;
using Unity;
using Unity.Microsoft.DependencyInjection;

namespace TrackerOfflineSearch;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
/// 
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
            .Configure<AppSettings>(config.GetSection("Application"))
            .AddLogging(loggingBuilder =>
                loggingBuilder
                    .AddConfiguration(config.GetSection("Logging"))
                    .AddDebug()
            );

        var container = new UnityContainer();
        container.BuildServiceProvider(serviceCollection);

        return new UnityContainerExtension(container);
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry
            .RegisterSingleton<IFileSystem, FileSystem>()
            .RegisterSingleton<IPlacementFactory, PlacementFactory>()
            .RegisterSingleton(typeof(IPlacement<>), typeof(Placement<>))
            .RegisterSingleton<Analyzer>(() => new StandardAnalyzer(AppConst.SearchEngineVersion))
            .RegisterSingleton<IPostMapper, PostMapper>()
            .RegisterSingleton<IArchiveManager, ArchiveManager>()
            .RegisterSingleton<IQueryBuilder, QueryBuilder>()
            .RegisterSingleton<IPostRepository, PostRepository>()
            .Register<IPostRepositoryWriter, PostRepositoryWriter>()
            .Register<IImportManager, ImportManager>()
            .RegisterSingleton<IBBTextConverter, BBTextConverter>()
            ;

        containerRegistry.RegisterDialog<RepositoryWizardView, RepositoryWizardViewModel>();
        
        containerRegistry.RegisterDialog<ForumSelectorView, ForumSelectorViewModel>();
        containerRegistry.RegisterDialogWindow<ForumSelectorWindow>(nameof(ForumSelectorWindow));
    }

    protected override Window CreateShell() => this.Container.Resolve<MainWindow>();

    protected override void InitializeShell(Window shell)
    {
        Lucene.Net.Util.InfoStream.Default = this.Container.Resolve<DebugInfoStream>();

        var regionManager = this.Container.Resolve<IRegionManager>();
        regionManager
            // Main views
            .RegisterViewWithRegion(RegionNames.SearchFormRegion, typeof(QueryEditorView))
            .RegisterViewWithRegion(RegionNames.SearchResultRegion, typeof(SearchResultView))
            .RegisterViewWithRegion(RegionNames.DatabaseToolsRegion, typeof(DatabaseToolsView))
            .RegisterViewWithRegion(RegionNames.PostInfoViewRegion, typeof(PostInfoView))
            ;

        base.InitializeShell(shell);
    }
}
