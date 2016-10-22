using System;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using Autofac;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.View;
using Remembrance.Card.ViewModel;
using Remembrance.Card.ViewModel.Contracts.Data;
using Remembrance.DAL;
using Remembrance.DAL.Contracts;
using Remembrance.Resources;
using Remembrance.Settings.View;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel;
using Remembrance.Translate.Yandex;
using Remembrance.TypeAdapter;
using Remembrance.WebApi;
using Scar.Common.IO;
using Scar.Common.Logging;
using Scar.Common.WPF;
using Scar.Common.WPF.Localization;

namespace Remembrance
{
    public partial class App
    {
        private static readonly string appGuid = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";
        private ILifetimeScope container;
        private IMessenger messenger;
        private Mutex mutex;

        protected override void OnStartup([NotNull] StartupEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            RegisterDependencies();

            CultureUtilities.ChangeCulture(container.Resolve<ISettingsRepository>().Get().UiLanguage);

            messenger = container.Resolve<IMessenger>();
            messenger.Register<string>(this, MessengerTokens.UiLanguageToken, CultureUtilities.ChangeCulture);
            messenger.Register<string>(this, MessengerTokens.UserMessageToken, message => MessageBox.Show(message, nameof(Remembrance), MessageBoxButton.OK, MessageBoxImage.Information));
            messenger.Register<string>(this, MessengerTokens.UserWarningToken, message => MessageBox.Show(message, nameof(Remembrance), MessageBoxButton.OK, MessageBoxImage.Warning));
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var mutexSecurity = new MutexSecurity();
            mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.FullControl, AccessControlType.Allow));
            mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.ChangePermissions, AccessControlType.Deny));
            mutexSecurity.AddAccessRule(new MutexAccessRule(sid, MutexRights.Delete, AccessControlType.Deny));
            bool createdNew;
            mutex = new Mutex(false, $"Global\\{nameof(Remembrance)}-{appGuid}", out createdNew, mutexSecurity);

            if (!mutex.WaitOne(0, false))
            {
                messenger.Send(Errors.AlreadyRunning, MessengerTokens.UserWarningToken);
                return;
            }

            container.Resolve<ITrayWindow>().ShowDialog();
            container.Resolve<IAssessmentCardManager>();
            container.Resolve<ApiHoster>();
        }

        private void RegisterDependencies()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();

            var builder = new ContainerBuilder();

            //TODO: Use Autofac Factory
            builder.RegisterGeneric(typeof(WindowFactory<>)).SingleInstance();
            builder.RegisterType<Messenger>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(ViewModelAdapter).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(WordsTranslator).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiHoster>().AsSelf().SingleInstance();
            builder.RegisterType<OpenFileService>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SaveFileService>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterAssemblyTypes(typeof(AssessmentCardViewModel).Assembly).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(SettingsViewModel).Assembly).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(SettingsWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(WordViewModel).Assembly).AsSelf().InstancePerDependency();

            builder.RegisterModule<LoggingModule>();

            container = builder.Build();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            container.Dispose();
            mutex.Dispose();
        }
    }
}