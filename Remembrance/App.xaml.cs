using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core.CardManagement;
using Remembrance.DAL;
using Remembrance.Resources;
using Remembrance.View.Card;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Settings;
using Remembrance.ViewModel.Settings.Data;
using Remembrance.WebApi;
using Scar.Common;
using Scar.Common.IO;
using Scar.Common.Logging;
using Scar.Common.Messages;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View;

namespace Remembrance
{
    public sealed partial class App
    {
        // TODO: Move to library
        private static readonly string AppGuid = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";

        [NotNull]
        private readonly ILifetimeScope _container;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly Mutex _mutex;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [CanBeNull]
        private SynchronizationContext _synchronizationContext;

        public App()
        {
            _container = RegisterDependencies();

            CultureUtilities.ChangeCulture(_container.Resolve<ISettingsRepository>().Get().UiLanguage);

            _messenger = _container.Resolve<IMessageHub>();
            _subscriptionTokens.Add(_messenger.Subscribe<Language>(language => CultureUtilities.ChangeCulture(language.Code)));
            _subscriptionTokens.Add(_messenger.Subscribe<Message>(ShowMessage));

            _logger = _container.Resolve<ILog>();
            _mutex = CreateMutex();

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, [NotNull] DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);

            // Prevent default unhandled exception processing
            e.Handled = true;
        }

        [NotNull]
        private Mutex CreateMutex()
        {
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var mutexSecurity = new MutexSecurity();
            mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
            mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
            mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));
            return new Mutex(false, $"Global\\{nameof(Remembrance)}-{AppGuid}", out _, mutexSecurity);
        }

        private void HandleException(Exception e)
        {
            //TODO: implement such handling in other projects
            if (e is OperationCanceledException)
            {
                _logger.Trace("Operation cancelled", e);
            }
            else
            {
                _logger.Fatal("Unhandled exception", e);
                NotifyError(e);
            }
        }

        private void NotifyError(Exception e)
        {
            var exception = e.GetMostInnerException();
            var message = exception.ToMessage();
            _messenger.Publish(message);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _container.Dispose();
            _mutex.Dispose();
            foreach (var token in _subscriptionTokens)
                _messenger.UnSubscribe(token);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!_mutex.WaitOne(0, false))
            {
                _messenger.Publish(Errors.AlreadyRunning.ToWarning());
                Current.Shutdown();
                return;
            }

            _synchronizationContext = SynchronizationContext.Current;

            _container.Resolve<ITrayWindow>().ShowDialog();
            _container.Resolve<IAssessmentCardManager>();

            // Need to create first instance of this class in the UI thread (for proper SyncContext)
            _container.Resolve<ITranslationResultCardManager>();
            _container.Resolve<ApiHoster>();
        }

        [NotNull]
        private static ILifetimeScope RegisterDependencies()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();

            var builder = new ContainerBuilder();

            // TODO: Use Autofac Factory
            builder.RegisterGeneric(typeof(WindowFactory<>)).SingleInstance();
            builder.RegisterInstance(MessageHub.Instance).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiHoster>().AsSelf().SingleInstance();
            builder.RegisterType<OpenFileService>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SaveFileService>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterAssemblyTypes(typeof(AssessmentCardViewModel).Assembly).AsSelf().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterModule<LoggingModule>();

            return builder.Build();
        }

        private void ShowMessage([NotNull] Message message)
        {
            var viewModel = _container.Resolve<MessageViewModel>(new TypedParameter(typeof(Message), message));
            if (_synchronizationContext == null)
                throw new InvalidOperationException("Sync context is not initialized");
            _synchronizationContext.Post(x => _container.Resolve<IMessageWindow>(new TypedParameter(typeof(MessageViewModel), viewModel)).Restore(), null);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, [NotNull] UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception.InnerException);
            e.SetObserved();
            e.Exception.Handle(ex => true);
        }
    }
}