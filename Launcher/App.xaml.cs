using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.Classification.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.ProcessMonitoring;
using Remembrance.Contracts.Sync;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core.CardManagement;
using Remembrance.Core.Classification;
using Remembrance.Core.ImageSearch;
using Remembrance.Core.ImageSearch.Qwant;
using Remembrance.Core.Sync;
using Remembrance.Core.Translation.Yandex;
using Remembrance.DAL.SharedBetweenMachines;
using Remembrance.View.Controls;
using Remembrance.ViewModel;
using Remembrance.WebApi.Controllers;
using Remembrance.Windows.Common;
using Scar.Common;
using Scar.Common.ApplicationLifetime;
using Scar.Common.Async;
using Scar.Common.AutofacHttpClientProvider;
using Scar.Common.DAL;
using Scar.Common.DAL.Model;
using Scar.Common.Messages;
using Scar.Common.MVVM.Commands;
using Scar.Common.Sync.Windows;
using Scar.Common.View;
using Scar.Common.WebApi;
using Scar.Common.WPF.CollectionView;
using Scar.Common.WPF.Controls.AutoCompleteTextBox.Provider;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.Startup;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[assembly: AssemblyCompany("Scar")]
[assembly: AssemblyCopyright("Copyright © Scar 2016")]
[assembly: Guid("a3f513c3-1c4f-4b36-aa44-16619cbaf174")]
[assembly: AssemblyProduct("Remembrance")]

namespace Remembrance.Launcher
{
    // TODO: Feature Store Learning info not for TranEntry, but for the particular PartOfSpeechTranslation or even more detailed.
    // TODO: Feature: if the word level is low, replace textbox with dropdown
    sealed partial class App
    {
        public App() : base(
            hostBuilder => ApiHostingHelper.RegisterWebApiHost(hostBuilder, new Uri("http://localhost:2053")).UseNLog(),
            services =>
            {
                services.AddHttpClient();
                ApiHostingHelper.RegisterServices(services, typeof(WordsController).Assembly);
            },
            (h, loggingBuilder) =>
            {
                NLogBuilder.ConfigureNLog("NLog.config");
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            },
            newInstanceHandling: NewInstanceHandling.Restart)
        {
            InitializeComponent();
        }

        protected override async Task OnStartupAsync()
        {
            var logger = Container.Resolve<ILogger<App>>();
            logger.LogTrace("Starting...");
            RegisterLiteDbCustomTypes();
            Current.Resources.MergedDictionaries.Add(new ResourceDictionary { { "SuggestionProvider", Container.Resolve<IAutoCompleteDataProvider>() } });

            // First the tray icon should be loaded, the rest can be loaded later
            var trayWindow = Container.Resolve<ITrayWindow>();
            trayWindow.ShowDialog();

            logger.LogInformation("Tray window is loaded");

            var tasks = new[]
            {
                ResolveInSeparateTaskAsync<ISynchronizationManager>(),
                ResolveInSeparateTaskAsync<IAssessmentCardManager>(),
                ResolveInSeparateTaskAsync<IActiveProcessMonitor>(),
                ResolveInSeparateTaskAsync<ISharedRepositoryCloner>()
            };
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogManager.Shutdown();
            base.OnExit(e);
        }

        protected override void RegisterDependencies(ContainerBuilder builder)
        {
            RegisterHttpClients(builder);
            RegisterCustomTypes(builder);
            RegisterRepositorySynchronizers(builder);
            RegisterCore(builder);
            RegisterDAL(builder);
            RegisterGenericWindowCreators(builder);
            RegisterViewModels(builder);
            RegisterView(builder);
        }

        protected override void ShowMessage(Message message)
        {
            var nestedLifeTimeScope = Container.BeginLifetimeScope();
            var viewModel = nestedLifeTimeScope.Resolve<MessageViewModel>(new TypedParameter(typeof(Message), message));
            var synchronizationContext = SynchronizationContext ?? SynchronizationContext.Current ?? throw new InvalidOperationException("SynchronizationContext.Current is null");
            synchronizationContext.Post(
                x =>
                {
                    var window = nestedLifeTimeScope.Resolve<IMessageWindow>(new TypedParameter(typeof(MessageViewModel), viewModel));
                    window.AssociateDisposable(nestedLifeTimeScope);
                    window.Restore();
                },
                null);
        }

        static void RegisterCustomTypes(ContainerBuilder builder)
        {
            builder.RegisterType<CollectionViewSourceAdapter>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterGeneric(typeof(WindowFactory<>)).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AutofacScopedWindowProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CultureManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationTerminator>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AutofacScopedWindowProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AutofacNamedInstancesFactory>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApplicationCommandManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(WindowPositionAdjustmentManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(WindowsSyncSoftwarePathsProvider).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CancellationTokenSourceProvider>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<RateLimiter>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType(typeof(DeletionEventsSyncExtender<TranslationEntry, TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryRepository, ITranslationEntryDeletionRepository>))
                .SingleInstance()
                .AsImplementedInterfaces();
        }

        static void RegisterView(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardControl).Assembly).AsImplementedInterfaces().InstancePerDependency();
        }

        static void RegisterViewModels(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardViewModel).Assembly)
                .Where(t => t.Name != "ProcessedByFody")
                .AsSelf() // For ViewModels
                .AsImplementedInterfaces() // ForWindowCreators //TODO: Separate assembly
                .InstancePerDependency();
        }

        static void RegisterCore(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
        }

        static void RegisterDAL(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
        }

        static void RegisterGenericWindowCreators(ContainerBuilder builder)
        {
            builder.RegisterType<GenericWindowCreator<IDictionaryWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<ISplashScreenWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<IAddTranslationWindow>>().AsImplementedInterfaces().SingleInstance();
        }

        static void RegisterRepositorySynchronizers(ContainerBuilder builder)
        {
            RegisterRepositorySynchronizer<ApplicationSettings, string, ISharedSettingsRepository, SharedSettingsRepository>(builder);
            RegisterRepositorySynchronizer<LearningInfo, TranslationEntryKey, ILearningInfoRepository, LearningInfoRepository>(builder);
            RegisterRepositorySynchronizer<TranslationEntry, TranslationEntryKey, ITranslationEntryRepository, TranslationEntryRepository>(builder);
            RegisterRepositorySynchronizer<TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryDeletionRepository, TranslationEntryDeletionRepository>(builder);
            RegisterRepositorySynchronizer<WordImageSearchIndex, WordKey, IWordImageSearchIndexRepository, WordImageSearchIndexRepository>(builder);
        }

        static void RegisterHttpClients(ContainerBuilder builder)
        {
            builder.RegisterHttpClient<WordsTranslator>(WordsTranslator.BaseAddress);
            builder.RegisterHttpClient<TextToSpeechPlayer>(TextToSpeechPlayer.BaseAddress);
            builder.RegisterHttpClient<Predictor>(Predictor.BaseAddress);
            builder.RegisterHttpClient<LanguageDetector>(LanguageDetector.BaseAddress);
            builder.RegisterHttpClient<UClassifyTopicsClient>(
                UClassifyTopicsClient.BaseAddress,
                client =>
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", UClassifyTopicsClient.Token);
                });
            builder.RegisterHttpClient<ImageSearcher>(
                ImageSearcher.BaseAddress,
                client =>
                {
                    client.DefaultRequestHeaders.Add("UserAgent", ImageSearcher.UserAgent);
                });
            builder.RegisterHttpClient<ImageDownloader>();
        }

        static void RegisterLiteDbCustomTypes()
        {
            RegisterLiteDbReadonlyCollection<PartOfSpeechTranslation>();
            RegisterLiteDbReadonlyCollection<TranslationVariant>();
            RegisterLiteDbReadonlyCollection<Example>();
            RegisterLiteDbReadonlyCollection<TextEntry>();
            RegisterLiteDbReadonlyCollection<Word>();
            RegisterLiteDbReadonlyCollection<ManualTranslation>();
            RegisterLiteDbReadonlyCollection<ClassificationCategory>();
            RegisterLiteDbStringReadonlyCollection();
            RegisterLiteDbIntReadonlyCollection();
            RegisterLiteDbSet<BaseWord>();
        }

        static void RegisterLiteDbReadonlyCollection<T>()
            where T : class
        {
            BsonMapper.Global.RegisterType<IReadOnlyCollection<T>>(
                o => new BsonArray(o.Select(x => BsonMapper.Global.ToDocument(x))),
                m => m.AsArray.Select(item => BsonMapper.Global.ToObject<T>(item.AsDocument)).ToArray());
        }

        static void RegisterLiteDbSet<T>()
            where T : class
        {
            BsonMapper.Global.RegisterType<ISet<T>>(
                o => new BsonArray(o.Select(x => BsonMapper.Global.ToDocument(x))),
                m => new HashSet<T>(m.AsArray.Select(item => BsonMapper.Global.ToObject<T>(item.AsDocument))));
        }

        static void RegisterLiteDbStringReadonlyCollection()
        {
            BsonMapper.Global.RegisterType<IReadOnlyCollection<string>>(o => new BsonArray(o.Select(x => x == null ? null : new BsonValue(x))), m => m.AsArray.Select(item => item.AsString).ToArray());
        }

        static void RegisterLiteDbIntReadonlyCollection()
        {
            BsonMapper.Global.RegisterType<IReadOnlyCollection<int?>>(
                o => new BsonArray(o.Select(x => x == null ? null : new BsonValue(x))),
                m => m.AsArray.Select(item => item.AsInt32).Cast<int?>().ToArray());
        }

        static void RegisterNamed<T, TInterface>(ContainerBuilder builder)
            where T : TInterface
            where TInterface : class
        {
            builder.RegisterType<T>().Named<TInterface>(typeof(TInterface).FullName ?? throw new InvalidOperationException("Interface FullName is null")).As<TInterface>().InstancePerDependency();
        }

        static void RegisterRepositorySynchronizer<TEntity, TId, TRepositoryInterface, TRepository>(ContainerBuilder builder)
            where TRepository : TRepositoryInterface, IChangeableRepository
            where TRepositoryInterface : class, IRepository<TEntity, TId>, ITrackedRepository, IFileBasedRepository, IDisposable
            where TEntity : IEntity<TId>, ITrackedEntity
        {
            builder.RegisterType(typeof(RepositorySynhronizer<TEntity, TId, TRepositoryInterface>)).SingleInstance().AsImplementedInterfaces();
            RegisterNamed<TRepository, TRepositoryInterface>(builder);
        }

        async Task ResolveInSeparateTaskAsync<T>()
            where T : class
        {
            await Task.Run(() => Container.Resolve<T>()).ConfigureAwait(false);
        }
    }
}
