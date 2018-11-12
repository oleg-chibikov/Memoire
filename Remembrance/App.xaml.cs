using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using JetBrains.Annotations;
using LiteDB;
using Microsoft.Win32;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.ProcessMonitoring;
using Remembrance.Contracts.Sync;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Contracts.View.Settings;
using Remembrance.Core;
using Remembrance.Core.CardManagement;
using Remembrance.Core.Sync;
using Remembrance.DAL.Shared;
using Remembrance.Resources;
using Remembrance.View;
using Remembrance.ViewModel;
using Remembrance.WebApi;
using Scar.Common;
using Scar.Common.Async;
using Scar.Common.DAL;
using Scar.Common.DAL.Model;
using Scar.Common.Messages;
using Scar.Common.WPF.Controls.AutoCompleteTextBox.Provider;
using Scar.Common.WPF.Startup;
using Scar.Common.WPF.View;

namespace Remembrance
{
    // TODO: Feature Store Learning info not for TranEntry, but for the particular PartOfSpeechTranslation or even more detailed.
    // TODO: Feature: if the word level is low, replace textbox with dropdown

    /// <summary>
    /// The app.
    /// </summary>
    internal sealed partial class App
    {
        [NotNull]
        private const string JsonFilesFilter = "Json files (*.json)|*.json;";

        [NotNull]
        private static readonly string DefaultFilePattern = $"{nameof(Remembrance)}.json";

        protected override string AlreadyRunningCaption { get; } = Errors.DefaultError;

        protected override NewInstanceHandling NewInstanceHandling => NewInstanceHandling.Restart;

        protected override void OnStartup()
        {
            RegisterLiteDbCustomTypes();
            Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    { "SuggestionProvider", Container.Resolve<IAutoCompleteDataProvider>() }
                });
            Container.Resolve<ITrayWindow>().ShowDialog();

            ResolveInSeparateTaskAsync<ISynchronizationManager>();
            ResolveInSeparateTaskAsync<ApiHoster>();
            ResolveInSeparateTaskAsync<IAssessmentCardManager>();
            ResolveInSeparateTaskAsync<IActiveProcessMonitor>();
            ResolveInSeparateTaskAsync<ISharedRepositoryCloner>();
        }

        private static void RegisterLiteDbCustomTypes()
        {
            RegisterLiteDbReadonlyCollection<PartOfSpeechTranslation>();
            RegisterLiteDbReadonlyCollection<TranslationVariant>();
            RegisterLiteDbReadonlyCollection<Example>();
            RegisterLiteDbReadonlyCollection<TextEntry>();
            RegisterLiteDbReadonlyCollection<Word>();
            RegisterLiteDbReadonlyCollection<ManualTranslation>();
            RegisterLiteDbStringReadonlyCollection();
            RegisterLiteDbSet<BaseWord>();
        }

        private static void RegisterLiteDbReadonlyCollection<T>()
            where T : class
        {
            BsonMapper.Global.RegisterType<IReadOnlyCollection<T>>(
                o => new BsonValue(o.Select(x => BsonMapper.Global.ToDocument(x))),
                m => m.AsArray.Select(item => BsonMapper.Global.ToObject<T>(item.AsDocument)).ToArray());
        }

        private static void RegisterLiteDbSet<T>()
            where T : class
        {
            BsonMapper.Global.RegisterType<ISet<T>>(
                o => new BsonValue(o.Select(x => BsonMapper.Global.ToDocument(x))),
                m => new HashSet<T>(m.AsArray.Select(item => BsonMapper.Global.ToObject<T>(item.AsDocument))));
        }

        private static void RegisterLiteDbStringReadonlyCollection()
        {
            BsonMapper.Global.RegisterType<IReadOnlyCollection<string>>(
                o => new BsonValue(o.Select(x => new BsonValue(x))),
                m => m.AsArray.Select(item => item.AsString).ToArray());
        }

        protected override void RegisterDependencies([NotNull] ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(WindowFactory<>)).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AutofacScopedWindowProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AutofacNamedInstancesFactory>().AsImplementedInterfaces().SingleInstance();

            RegisterRepositorySynchronizer<Settings, string, ISettingsRepository, SettingsRepository>(builder);
            RegisterRepositorySynchronizer<LearningInfo, TranslationEntryKey, ILearningInfoRepository, LearningInfoRepository>(builder);
            RegisterRepositorySynchronizer<TranslationEntry, TranslationEntryKey, ITranslationEntryRepository, TranslationEntryRepository>(builder);
            RegisterRepositorySynchronizer<TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryDeletionRepository, TranslationEntryDeletionRepository>(builder);
            RegisterRepositorySynchronizer<WordImageSearchIndex, WordKey, IWordImageSearchIndexRepository, WordImageSearchIndexRepository>(builder);

            builder.RegisterType(
                    typeof(DeletionEventsSyncExtender<TranslationEntry, TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryRepository,
                            ITranslationEntryDeletionRepository>
                    ))
                .SingleInstance()
                .AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<IDictionaryWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<ISplashScreenWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<IAddTranslationWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiHoster>().AsSelf().SingleInstance();

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
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardViewModel).Assembly).Where(t => t.Name != "ProcessedByFody").AsSelf().InstancePerDependency();
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<CancellationTokenSourceProvider>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<RateLimiter>().AsImplementedInterfaces().InstancePerDependency();
        }

        protected override void ShowMessage(Message message)
        {
            var nestedLifeTimeScope = Container.BeginLifetimeScope();
            var viewModel = nestedLifeTimeScope.Resolve<MessageViewModel>(new TypedParameter(typeof(Message), message));
            var synchronizationContext = SynchronizationContext ?? throw new InvalidOperationException();
            synchronizationContext.Post(
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
            builder.RegisterType<T>().Named<TInterface>(typeof(TInterface).FullName).As<TInterface>().InstancePerDependency();
        }

        private static void RegisterRepositorySynchronizer<TEntity, TId, TRepositoryInterface, TRepository>([NotNull] ContainerBuilder builder)
            where TRepository : TRepositoryInterface, IChangeableRepository
            where TRepositoryInterface : IRepository<TEntity, TId>, ITrackedRepository, IFileBasedRepository, IDisposable
            where TEntity : IEntity<TId>, ITrackedEntity
        {
            builder.RegisterType(typeof(RepositorySynhronizer<TEntity, TId, TRepositoryInterface>)).SingleInstance().AsImplementedInterfaces();
            RegisterNamed<TRepository, TRepositoryInterface>(builder);
        }

        private void ResolveInSeparateTaskAsync<T>()
        {
            Task.Run(() => Container.Resolve<T>());
        }
    }
}