using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using LiteDB;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.View.Settings;
using Mémoire.Core.CardManagement;
using Mémoire.Core.Sync;
using Mémoire.DAL.SharedBetweenMachines;
using Mémoire.View.Windows;
using Mémoire.ViewModel;
using Mémoire.WindowCreators;
using Mémoire.Windows.Common;
using Scar.Common.Async;
using Scar.Common.AutofacHttpClientProvision;
using Scar.Common.AutofacInstantiation;
using Scar.Common.DAL.Contracts;
using Scar.Common.DAL.Contracts.Model;
using Scar.Common.MVVM.Commands;
using Scar.Common.RateLimiting;
using Scar.Common.Sync.Windows;
using Scar.Common.View.AutofacWindowProvision;
using Scar.Common.View.WindowCreation;
using Scar.Common.WPF.CollectionView;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.Startup;
using Scar.Common.WPF.View.WindowCreation;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data.Classification;
using Scar.Services.Contracts.Data.ExtendedTranslation;
using Scar.Services.Contracts.Data.Translation;
using Scar.Services.ImageDownload;
using Scar.Services.Qwant;
using Scar.Services.UClassify;
using Scar.Services.Yandex;
using LanguageDetector = Mémoire.Core.Languages.LanguageDetector;

namespace Mémoire.Launcher
{
    static class RegistrationExtensions
    {
        public static ContainerBuilder RegisterServices(this ContainerBuilder builder)
        {
            builder.Register(context => context.Resolve<ISharedSettingsRepository>().ClassificationMinimalThreshold).Named<double>(nameof(ISharedSettingsRepository.ClassificationMinimalThreshold));
            builder.Register(context => context.Resolve<ISharedSettingsRepository>().ApiKeys.YandexTextToSpeech).Named<string>(nameof(ApiKeys.YandexTextToSpeech));
            builder.Register(context => context.Resolve<ISharedSettingsRepository>().ApiKeys.UClassify).Named<string>(nameof(ApiKeys.UClassify));

            builder.RegisterType<UClassifyTopicsClient>()
                .WithParameter(ResolvedParameter.ForNamed<string>(nameof(ApiKeys.UClassify)))
                .AsImplementedInterfaces()
                .SingleInstance()
                .Named<IClassificationClient>(nameof(UClassifyTopicsClient));
            builder.RegisterType<UClassifyHierarchicalTopicsClient>()
                .WithParameter(ResolvedParameter.ForNamed<IClassificationClient>(nameof(UClassifyTopicsClient)))
                .WithParameter(ResolvedParameter.ForNamed<double>(nameof(SharedSettingsRepository.ClassificationMinimalThreshold)))
                .AsImplementedInterfaces()
                .SingleInstance();
            builder.RegisterType<WordsTranslator>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TextToSpeechPlayer>().WithParameter(ResolvedParameter.ForNamed<string>(nameof(ApiKeys.YandexTextToSpeech))).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<Predictor>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<LanguageDetector>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ImageDownloader>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ImageSearcher>().AsImplementedInterfaces().SingleInstance();
            return builder;
        }

        public static ContainerBuilder RegisterCustomTypes(this ContainerBuilder builder)
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
            builder.RegisterType<WindowDisplayer>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<StaThreadWindowDisplayer>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CancellationTokenSourceProvider>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<RateLimiter>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType(typeof(DeletionEventsSyncExtender<TranslationEntry, TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryRepository, ITranslationEntryDeletionRepository>))
                .SingleInstance()
                .AsImplementedInterfaces();
            return builder;
        }

        public static ContainerBuilder RegisterView(this ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(DictionaryWindow).Assembly).AsImplementedInterfaces().InstancePerDependency();
            return builder;
        }

        public static ContainerBuilder RegisterViewModels(this ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(AssessmentTextInputCardViewModel).Assembly)
                .Where(x => !x.Name.Contains("ProcessedByFody", StringComparison.OrdinalIgnoreCase)).AsSelf()
                .InstancePerDependency();

            return builder;
        }

        public static ContainerBuilder RegisterCore(this ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(AssessmentCardManager).Assembly).AsImplementedInterfaces().SingleInstance();
            return builder;
        }

        public static ContainerBuilder RegisterDAL(this ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(TranslationEntryRepository).Assembly).AsImplementedInterfaces().SingleInstance();
            return builder;
        }

        public static ContainerBuilder RegisterGenericWindowCreators(this ContainerBuilder builder)
        {
            builder.RegisterType<GenericWindowCreator<IDictionaryWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<ILoadingWindow>>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GenericWindowCreator<IAddTranslationWindow>>().AsImplementedInterfaces().SingleInstance();
            return builder;
        }

        public static ContainerBuilder RegisterNamedWindowCreators(this ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(AssessmentBatchCardWindowCreator).Assembly).AsImplementedInterfaces().InstancePerDependency();
            return builder;
        }

        public static ContainerBuilder RegisterRepositorySynchronizers(this ContainerBuilder builder)
        {
            builder.RegisterRepositorySynchronizer<ApplicationSettings, string, ISharedSettingsRepository, SharedSettingsRepository>()
                .RegisterRepositorySynchronizer<LearningInfo, TranslationEntryKey, ILearningInfoRepository, LearningInfoRepository>()
                .RegisterRepositorySynchronizer<TranslationEntry, TranslationEntryKey, ITranslationEntryRepository, TranslationEntryRepository>()
                .RegisterRepositorySynchronizer<TranslationEntryDeletion, TranslationEntryKey, ITranslationEntryDeletionRepository, TranslationEntryDeletionRepository>()
                .RegisterRepositorySynchronizer<WordImageSearchIndex, WordKey, IWordImageSearchIndexRepository, WordImageSearchIndexRepository>();
            return builder;
        }

        public static ContainerBuilder RegisterHttpClients(this ContainerBuilder builder)
        {
            builder.RegisterHttpClient<WordsTranslator>();
            builder.RegisterHttpClient<TextToSpeechPlayer>();
            builder.RegisterHttpClient<Predictor>();
            builder.RegisterHttpClient<UClassifyTopicsClient>();
            builder.RegisterHttpClient<ImageSearcher>();
            builder.RegisterHttpClient<ImageDownloader>();
            return builder;
        }

        public static void RegisterLiteDbCustomTypes()
        {
            RegisterLiteDbReadonlyCollection<ExtendedPartOfSpeechTranslation>();
            RegisterLiteDbReadonlyCollection<ExtendedExample>();
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

        static ContainerBuilder RegisterNamed<T, TInterface>(this ContainerBuilder builder)
            where T : TInterface
            where TInterface : class
        {
            builder.RegisterType<T>().Named<TInterface>(typeof(TInterface).FullName ?? throw new InvalidOperationException("Interface FullName is null")).As<TInterface>().InstancePerDependency();
            return builder;
        }

        static ContainerBuilder RegisterRepositorySynchronizer<TEntity, TId, TRepositoryInterface, TRepository>(this ContainerBuilder builder)
            where TRepository : TRepositoryInterface, IChangeableRepository
            where TRepositoryInterface : class, IRepository<TEntity, TId>, ITrackedRepository, IFileBasedRepository, IDisposable
            where TEntity : IEntity<TId>, ITrackedEntity
        {
            builder.RegisterType(typeof(RepositorySynhronizer<TEntity, TId, TRepositoryInterface>)).SingleInstance().AsImplementedInterfaces();
            return builder.RegisterNamed<TRepository, TRepositoryInterface>();
        }
    }
}
