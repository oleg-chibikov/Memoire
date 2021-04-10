using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.ProcessMonitoring;
using Mémoire.Contracts.Sync;
using Mémoire.Contracts.View.Settings;
using Mémoire.Core;
using Mémoire.View.Windows;
using Mémoire.ViewModel;
using Mémoire.WebApi.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scar.Common.ApplicationLifetime.Core;
using Scar.Common.Messages;
using Scar.Common.WebApi;
using Scar.Common.WPF.Controls;
using Scar.Common.WPF.View.WindowCreation;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[assembly: Guid("a3f513c3-1c4f-4b36-aa44-16619cbaf174")]

namespace Mémoire.Launcher
{
    // TODO: Feature Store Learning info not for TranEntry, but for the particular PartOfSpeechTranslation or even more detailed.
    // TODO: Feature: if the word level is low, replace textbox with dropdown
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "SplashScreenWindow is disposed during startup")]
    sealed partial class App
    {
        readonly Action _closeSplashScreen;

        public App() : base(
            hostBuilder => ApiHostingHelper.RegisterWebApiHost(hostBuilder, new Uri("http://localhost:2053")),
            services =>
            {
                services.AddHttpClient();
                ApiHostingHelper.RegisterServices(services, typeof(WordsController).Assembly);
            },
            (h, loggingBuilder) =>
            {
                if (File.Exists(PathsProvider.LogsPath))
                {
                    File.Delete(PathsProvider.LogsPath);
                }

                Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.File(PathsProvider.LogsPath).MinimumLevel.Verbose().CreateLogger();

                loggingBuilder.ClearProviders().AddSerilog();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            },
            newInstanceHandling: NewInstanceHandling.Restart)
        {
            InitializeComponent();
            var staThreadWindowDisplayer = new StaThreadWindowDisplayer();
            var executeInSplashScreenWindowContext = staThreadWindowDisplayer.DisplayWindow(() => new SplashScreenWindow());
            _closeSplashScreen = () => executeInSplashScreenWindowContext(
                window =>
                {
                    window?.Close();
                    window?.Dispose();
                });
        }

        protected override async Task OnStartupAsync()
        {
            var logger = Container.Resolve<ILogger<App>>();
            logger.LogTrace("Starting...");
            RegistrationExtensions.RegisterLiteDbCustomTypes();
            Current.Resources.MergedDictionaries.Add(new ResourceDictionary { { "SuggestionProvider", Container.Resolve<IAutoCompleteDataProvider>() } });

            // First the tray icon should be loaded, the rest can be loaded later
            var trayWindow = Container.Resolve<ITrayWindow>();
            trayWindow.ShowDialog();

            logger.LogInformation("Tray window is loaded");

            var tasks = new[]
            {
                ResolveInSeparateTaskAsync<ISynchronizationManager>(logger),
                ResolveInSeparateTaskAsync<IAssessmentCardManager>(logger),
                ResolveInSeparateTaskAsync<IActiveProcessMonitor>(logger),
                ResolveInSeparateTaskAsync<ISharedRepositoryCloner>(logger)
            };
            await Task.WhenAll(tasks).ConfigureAwait(false);
            _closeSplashScreen();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        protected override void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterHttpClients()
                .RegisterCustomTypes()
                .RegisterRepositorySynchronizers()
                .RegisterCore()
                .RegisterDAL()
                .RegisterGenericWindowCreators()
                .RegisterNamedWindowCreators()
                .RegisterViewModels()
                .RegisterView()
                .RegisterServices();
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

        async Task ResolveInSeparateTaskAsync<T>(ILogger logger)
            where T : class
        {
            await Task.Run(() =>
            {
                var type = typeof(T).Name;
                logger.LogTrace($"Resolving {type}...");
                var instance = Container.Resolve<T>();
                logger.LogDebug($"Resolved {type}");
                return instance;
            }).ConfigureAwait(false);
        }
    }
}
