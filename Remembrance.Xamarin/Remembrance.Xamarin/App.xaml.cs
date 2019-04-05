using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core.CardManagement;
using Remembrance.DAL.Shared;
using Remembrance.ViewModel;
using Scar.Common.ApplicationLifetime;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.MVVM.Commands;
using Scar.Common.Sync;
using Scar.Common.View;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowFactory;

namespace Remembrance.Xamarin
{
    public sealed partial class App
    {
        public class NullSyncSoftwarePathsProvider : IOneDrivePathProvider, IDropBoxPathProvider
        {
            public string? GetDropBoxPath()
            {
                return null;
            }

            public string? GetOneDrivePath()
            {
                return null;
            }
        }

        class CommandManager : ICommandManager {
            public void AddRaiseCanExecuteChangedAction(ref Action raiseCanExecuteChangedAction)
            {
            }

            public void RemoveRaiseCanExecuteChangedAction(Action raiseCanExecuteChangedAction)
            {
            }

            public void AssignOnPropertyChanged(ref PropertyChangedEventHandler propertyEventHandler)
            {
            }

            public void RefreshCommandStates()
            {
            }
        }

        class EntryAssemblyProvider : IEntryAssemblyProvider
        {
            public Assembly ProvideEntryAssembly() => GetType().Assembly;
        }

        class NullWindowPositionAdjustmentManager : IWindowPositionAdjustmentManager
        {
            public NullWindowPositionAdjustmentManager()
            {
            }

            public void AdjustAnyWindowPosition(IDisplayable window)
            {
            }

            public void AdjustDetailsCardWindowPosition(IDisplayable window)
            {
            }

            public void AdjustActivatedWindow(IDisplayable window)
            {
            }
        }

        class LoadingWindow : ILoadingWindow
        {
            public event EventHandler SizeChanged;
            public event EventHandler Closed;
            public event EventHandler Loaded;

            public void AssociateDisposable(IDisposable disposable)
            {
                Loaded?.Invoke(this,null);
                Closed?.Invoke(this, null);
                SizeChanged?.Invoke(this, null);
            }

            public void Close()
            {
            }

            public void Restore()
            {
            }

            public void Show()
            {
            }

            public bool? ShowDialog()
            {
                return true;
            }

            public bool UnassociateDisposable(IDisposable disposable)
            {
                return true;
            }
        }

        class AddTranslationWindow : IAddTranslationWindow
        {
            public event EventHandler SizeChanged;
            public event EventHandler Closed;
            public event EventHandler Loaded;

            public void AssociateDisposable(IDisposable disposable)
            {
                Loaded?.Invoke(this, null);
                Closed?.Invoke(this, null);
                SizeChanged?.Invoke(this, null);
            }

            public void Close()
            {
            }

            public void Restore()
            {
            }

            public void Show()
            {
            }

            public bool? ShowDialog()
            {
                return true;
            }

            public bool UnassociateDisposable(IDisposable disposable)
            {
                return true;
            }
        }

        public App()
            : base(new AssemblyInfoProvider(new EntryAssemblyProvider(), new SpecialPathsProvider()))
        {
            InitializeComponent();
        }

        protected override void OnStartup()
        {
            Container.Resolve<ILocalSettingsRepository>();
            Container.Resolve<ILanguageManager>();
            Container.Resolve<ITranslationEntryProcessor>();
            Container.Resolve<ILog>();
            Container.Resolve<IWindowFactory<IAddTranslationWindow>>();
            Container.Resolve <ICommandManager>();

            var vm = Container.Resolve<AddTranslationViewModel>();
            MainPage = Container.Resolve<MainPage>();
        }

        protected override void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterType<LoadingWindow>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<AddTranslationWindow>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<NullWindowPositionAdjustmentManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AutofacScopedWindowProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterGeneric(typeof(WindowFactory<>)).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MainPage>().AsSelf().InstancePerDependency();
            builder.RegisterType<CommandManager>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<NullSyncSoftwarePathsProvider>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(SettingsRepository).Assembly).AsImplementedInterfaces().SingleInstance();
        }
    }
}
