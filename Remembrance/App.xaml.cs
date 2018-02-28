using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Common.WPF.Controls.AutoCompleteTextBox.Provider;
using JetBrains.Annotations;
using Microsoft.Win32;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Sync;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core.CardManagement;
using Remembrance.Core.Sync;
using Remembrance.DAL.Shared;
using Remembrance.Resources;
using Remembrance.View.Card;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Settings;
using Remembrance.WebApi;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Common.WPF.View;

namespace Remembrance
{
    /// <summary>
    /// The app.
    /// </summary>
    internal sealed partial class App
    {
        [NotNull]
        private const string JsonFilesFilter = "Json files (*.json)|*.json;";

        [NotNull]
        private static readonly string DefaultFilePattern = $"{nameof(Remembrance)}.json";

        protected override string AlreadyRunningMessage { get; } = Errors.AlreadyRunning;

        protected override string AppGuid { get; } = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";

        protected override void OnStartup()
        {
            Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    { "SuggestionProvider", Container.Resolve<IAutoCompleteDataProvider>() }
                });
            Container.Resolve<ITrayWindow>().ShowDialog();

            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = ResolveInSeparateTaskAsync<ISynchronizationManager>();

            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = ResolveInSeparateTaskAsync<ApiHoster>();

            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = ResolveInSeparateTaskAsync<IAssessmentCardManager>();
        }

        protected override void RegisterDependencies([NotNull] ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(WindowFactory<>)).AsSelf().SingleInstance();

            builder.RegisterType<INamedInstancesFactory>().AsImplementedInterfaces().SingleInstance();
            RegisterNamed<SettingsRepository, ISettingsRepository>(builder);
            RegisterNamed<LearningInfoRepository, ILearningInfoRepository>(builder);
            RegisterNamed<TranslationEntryRepository, ITranslationEntryRepository>(builder);
            RegisterNamed<TranslationEntryDeletionRepository, ITranslationEntryDeletionRepository>(builder);

            builder.RegisterType(typeof(RepositorySynhronizer<Settings, int, ISettingsRepository>)).SingleInstance().AsImplementedInterfaces();
            builder.RegisterType(typeof(RepositorySynhronizer<LearningInfo, TranslationEntryKey, ILearningInfoRepository>)).SingleInstance().AsImplementedInterfaces();
            builder.RegisterType(typeof(RepositorySynhronizer<TranslationEntry, TranslationEntryKey, ITranslationEntryRepository>)).SingleInstance().AsImplementedInterfaces();
            builder.RegisterType(typeof(RepositorySynhronizer<TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryDeletionRepository>)).SingleInstance().AsImplementedInterfaces();
            builder.RegisterType(typeof(DeletionEventsSyncExtender<TranslationEntry, TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryRepository, ITranslationEntryDeletionRepository>))
                .SingleInstance()
                .AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiHoster>().AsSelf().SingleInstance();

            // TODO: move autocomplete to library and nuget
            // builder.RegisterType<SuggestionProvider>().AsImplementedInterfaces().SingleInstance();
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
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardViewModel).Assembly).AsSelf().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<CancellationTokenSourceProvider>().AsImplementedInterfaces().InstancePerDependency();
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

        private static void RegisterNamed<T, TInterface>([NotNull] ContainerBuilder builder)
            where T : TInterface
        {
            builder.RegisterType<T>().Named<TInterface>(typeof(TInterface).FullName).AsImplementedInterfaces().InstancePerDependency();
        }

        [ItemNotNull]
        [NotNull]
        private async Task<T> ResolveInSeparateTaskAsync<T>()
        {
            return await Task.Run(() => Container.Resolve<T>()).ConfigureAwait(false);
        }
    }
}