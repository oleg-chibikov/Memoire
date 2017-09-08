using System;
using Autofac;
using JetBrains.Annotations;
using Mapster;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.ViewModel.Translation;

namespace Remembrance.Core
{
    [UsedImplicitly]
    public sealed class ViewModelAdapter : IViewModelAdapter
    {
        public ViewModelAdapter([NotNull] ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));

            TypeAdapterConfig<TranslationEntryViewModel, TranslationEntry>.NewConfig()
                .ConstructUsing(
                    src => new TranslationEntry
                    {
                        Key = new TranslationEntryKey(src.Text, src.Language, src.TargetLanguage)
                    })
                .Compile();
            TypeAdapterConfig<IWord, WordViewModel>.NewConfig().ConstructUsing(src => lifetimeScope.Resolve<WordViewModel>()).Compile();
            TypeAdapterConfig<IWord, PriorityWordViewModel>.NewConfig().ConstructUsing(src => lifetimeScope.Resolve<PriorityWordViewModel>()).Compile();
            TypeAdapterConfig<TranslationVariant, TranslationVariantViewModel>.NewConfig().ConstructUsing(src => lifetimeScope.Resolve<TranslationVariantViewModel>()).Compile();
            TypeAdapterConfig<PartOfSpeechTranslation, PartOfSpeechTranslationViewModel>.NewConfig().ConstructUsing(src => lifetimeScope.Resolve<PartOfSpeechTranslationViewModel>()).Compile();
            TypeAdapterConfig<TranslationEntry, TranslationEntryViewModel>.NewConfig()
                .ConstructUsing(src => lifetimeScope.Resolve<TranslationEntryViewModel>())
                .Map(dest => dest.Text, src => src.Key.Text)
                .Map(dest => dest.Language, src => src.Key.SourceLanguage)
                .Map(dest => dest.TargetLanguage, src => src.Key.TargetLanguage)
                .AfterMapping((src, dest) => dest.ReloadTranslations());
            TypeAdapterConfig<TranslationInfo, TranslationDetailsViewModel>.NewConfig()
                .ConstructUsing(src => new TranslationDetailsViewModel(lifetimeScope.Resolve<TranslationResultViewModel>()))
                .Map(dest => dest.TranslationResult, src => src.TranslationDetails.TranslationResult)
                .Map(dest => dest.Id, src => src.TranslationDetails.Id)
                .Map(dest => dest.TranslationEntryId, src => src.TranslationDetails.TranslationEntryId)
                .AfterMapping(
                    (src, dest) =>
                    {
                        foreach (var partOfSpeechTranslation in dest.TranslationResult.PartOfSpeechTranslations)
                        {
                            partOfSpeechTranslation.Language = src.Key.SourceLanguage;
                            foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                            {
                                SetPriorityWordProperties(translationVariant, src.Key.TargetLanguage, dest.TranslationEntryId);
                                if (translationVariant.Synonyms != null)
                                    foreach (var synonym in translationVariant.Synonyms)
                                        SetPriorityWordProperties(synonym, src.Key.TargetLanguage, dest.TranslationEntryId);

                                if (translationVariant.Meanings != null)
                                    foreach (var meaning in translationVariant.Meanings)
                                        meaning.Language = src.Key.SourceLanguage;
                            }
                        }
                    })
                .Compile();
        }

        public TDestination Adapt<TDestination>(object source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Adapt<TDestination>();
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (Equals(source, default(TSource)))
                throw new ArgumentNullException(nameof(source));
            if (Equals(destination, default(TDestination)))
                throw new ArgumentNullException(nameof(destination));

            return source.Adapt(destination, TypeAdapterConfig.GlobalSettings);
        }

        private static void SetPriorityWordProperties([NotNull] PriorityWordViewModel priorityWordViewModel, [NotNull] string targetLanguage, [NotNull] object translationEntryId)
        {
            priorityWordViewModel.SetProperties(translationEntryId, targetLanguage);
        }
    }
}