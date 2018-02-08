using System;
using Autofac;
using JetBrains.Annotations;
using Mapster;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.ViewModel.Translation;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    public sealed class ViewModelAdapter : IViewModelAdapter
    {
        private readonly TypeAdapterConfig _config;

        public ViewModelAdapter([NotNull] ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
            {
                throw new ArgumentNullException(nameof(lifetimeScope));
            }

            _config = new TypeAdapterConfig();

            _config.NewConfig<BaseWord, WordViewModel>().ConstructUsing(word => lifetimeScope.Resolve<WordViewModel>()).Compile();
            _config.NewConfig<BaseWord, PriorityWordViewModel>()
                .ConstructUsing(word => lifetimeScope.Resolve<PriorityWordViewModel>())
                .Map(priorityWordViewModel => priorityWordViewModel.Text, word => word.Text) //This is unexpected, but still needed
                .Compile();
            _config.NewConfig<TranslationVariant, TranslationVariantViewModel>().ConstructUsing(translationVariant => lifetimeScope.Resolve<TranslationVariantViewModel>()).Compile();
            _config.NewConfig<PartOfSpeechTranslation, PartOfSpeechTranslationViewModel>().ConstructUsing(partOfSpeechTranslation => lifetimeScope.Resolve<PartOfSpeechTranslationViewModel>()).Compile();
            _config.NewConfig<TranslationEntry, TranslationEntryViewModel>()
                .ConstructUsing(translationEntry => lifetimeScope.Resolve<TranslationEntryViewModel>(new TypedParameter(typeof(TranslationEntryKey), translationEntry.Id)))
                .AfterMapping((translationEntry, translationEntryViewModel) => translationEntryViewModel.ReloadTranslationsAsync(translationEntry).ConfigureAwait(false))
                .Compile();
            _config.NewConfig<TranslationInfo, TranslationDetailsViewModel>()
                .ConstructUsing(translationInfo => new TranslationDetailsViewModel(lifetimeScope.Resolve<TranslationResultViewModel>()))
                .Map(translationDetailsViewModel => translationDetailsViewModel.TranslationResult, translationInfo => translationInfo.TranslationDetails.TranslationResult)
                .Map(translationDetailsViewModel => translationDetailsViewModel.TranslationEntryKey, translationInfo => translationInfo.TranslationDetails.Id)
                .AfterMapping(
                    (translationInfo, translationDetailsViewModel) =>
                    {
                        foreach (var partOfSpeechTranslationViewModel in translationDetailsViewModel.TranslationResult.PartOfSpeechTranslations)
                        {
                            partOfSpeechTranslationViewModel.Language = translationInfo.TranslationEntryKey.SourceLanguage;
                            foreach (var translationVariantViewModel in partOfSpeechTranslationViewModel.TranslationVariants)
                            {
                                SetPriorityWordProperties(translationVariantViewModel, translationInfo.TranslationEntry, partOfSpeechTranslationViewModel.Text);
                                if (translationVariantViewModel.Synonyms != null)
                                {
                                    foreach (var synonym in translationVariantViewModel.Synonyms)
                                    {
                                        SetPriorityWordProperties(synonym, translationInfo.TranslationEntry, partOfSpeechTranslationViewModel.Text);
                                    }
                                }

                                if (translationVariantViewModel.Meanings == null)
                                {
                                    continue;
                                }

                                foreach (var meaning in translationVariantViewModel.Meanings)
                                {
                                    meaning.Language = translationInfo.TranslationEntryKey.SourceLanguage;
                                }
                            }
                        }
                    })
                .Compile();
        }

        public TDestination Adapt<TDestination>(object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Adapt<TDestination>(_config);
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (Equals(source, default(TSource)))
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (Equals(destination, default(TDestination)))
            {
                throw new ArgumentNullException(nameof(destination));
            }

            return source.Adapt(destination, _config);
        }

        private static void SetPriorityWordProperties([NotNull] PriorityWordViewModel priorityWordViewModel, [NotNull] TranslationEntry translationEntry, [NotNull] string partOfSpeechTranslationText)
        {
            priorityWordViewModel.SetTranslationEntryKey(translationEntry.Id, partOfSpeechTranslationText);
            priorityWordViewModel.SetIsPriority(translationEntry.PriorityWords?.Contains(priorityWordViewModel) == true);
        }
    }
}