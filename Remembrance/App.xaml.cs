using System.Windows;
using Autofac;
using Common.WPF.Controls.AutoCompleteTextBox.Provider;
using Microsoft.Win32;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.Sync;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core.CardManagement;
using Remembrance.DAL;
using Remembrance.Resources;
using Remembrance.View.Card;
using Remembrance.View.Various;
using Remembrance.ViewModel;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Settings;
using Remembrance.WebApi;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Common.WPF.View;

namespace Remembrance
{
    public sealed partial class App
    {
        private const string JsonFilesFilter = "Json files (*.json)|*.json;";
        private static readonly string DefaultFilePattern = $"{nameof(Remembrance)}.json";
        protected override string AppGuid { get; } = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";
        protected override string AlreadyRunningMessage { get; } = Errors.AlreadyRunning;

        protected override void OnStartup()
        {
            var myResourceDictionary = new ResourceDictionary
            {
                { "SuggestionProvider", Container.Resolve<IAutoCompleteDataProvider>() }
            };

            Current.Resources.MergedDictionaries.Add(myResourceDictionary);
            Container.Resolve<ITrayWindow>()
                .ShowDialog();
            Container.Resolve<IAssessmentCardManager>();
            // Need to create first instance of this class in the UI thread (for proper SyncContext)
            Container.Resolve<ITranslationDetailsCardManager>();
            Container.Resolve<ApiHoster>();
            Container.Resolve<ISynchronizationManager>();
        }

        protected override void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(WindowFactory<>))
                .SingleInstance();
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly)
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly)
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<ApiHoster>()
                .AsSelf()
                .SingleInstance();
            //TODO: move autocomplete to library and nuget
            builder.RegisterType<SuggestionProvider>()
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterInstance(
                    new OpenFileDialog
                    {
                        CheckFileExists = true,
                        FileName = DefaultFilePattern,
                        Filter = JsonFilesFilter,
                        RestoreDirectory = true,
                        Title = $"{Texts.Title}: {Texts.Import}"
                    })
                .AsSelf()
                .SingleInstance();
            builder.RegisterInstance(
                    new SaveFileDialog
                    {
                        FileName = DefaultFilePattern,
                        Filter = JsonFilesFilter,
                        RestoreDirectory = true,
                        Title = $"{Texts.Title}: {Texts.Export}"
                    })
                .AsSelf()
                .SingleInstance();
            //Including ViewModelAdapter
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardViewModel).Assembly)
                .Except<ViewModelAdapter>()
                .AsSelf()
                .InstancePerDependency();
            builder.RegisterType<ViewModelAdapter>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardWindow).Assembly)
                .AsImplementedInterfaces()
                .InstancePerDependency();
            builder.RegisterType<CancellationTokenSourceProvider>()
                .AsImplementedInterfaces()
                .InstancePerDependency();
        }

        protected override void ShowMessage(Message message)
        {
            var nestedLifeTimeScope = Container.BeginLifetimeScope();
            var viewModel = nestedLifeTimeScope.Resolve<MessageViewModel>(new TypedParameter(typeof(Message), message));
            SynchronizationContext.Post(
                x =>
                {
                    var window = nestedLifeTimeScope.Resolve<IMessageWindow>(new TypedParameter(typeof(MessageViewModel), viewModel));
                    window.AssociateDisposable(nestedLifeTimeScope);
                    window.Restore();
                },
                null);
        }
    }
}