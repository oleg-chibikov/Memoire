using System;
using Autofac;
using Easy.MessageHub;
using JetBrains.Annotations;
using Mapster;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.ViewModel.Translation;
using Scar.Common.Messages;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    public sealed class ViewModelAdapter : IViewModelAdapter
    {
        private readonly TypeAdapterConfig _config;
        public ViewModelAdapter([NotNull] ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));

            _config = new TypeAdapterConfig();

            _config.NewConfig<TranslationEntryViewModel, TranslationEntry>()
                .ConstructUsing(
                    translationEntryViewModel => new TranslationEntry
                    {
                        Key = new TranslationEntryKey(translationEntryViewModel.Text, translationEntryViewModel.Language, translationEntryViewModel.TargetLanguage)
                    })
                .Compile();
            _config.NewConfig<IWord, WordViewModel>()
                .ConstructUsing(word => lifetimeScope.Resolve<WordViewModel>())
                .Compile();
            _config.NewConfig<IWord, PriorityWordViewModel>()
                .ConstructUsing(word => lifetimeScope.Resolve<PriorityWordViewModel>())
                .Map(priorityWordViewModel => priorityWordViewModel.Text, word => word.Text) //This is unexpected, but still needed
                .Compile();
            _config.NewConfig<TranslationVariant, TranslationVariantViewModel>()
                .ConstructUsing(translationVariant => lifetimeScope.Resolve<TranslationVariantViewModel>())
                .Compile();
            _config.NewConfig<PartOfSpeechTranslation, PartOfSpeechTranslationViewModel>()
                .ConstructUsing(partOfSpeechTranslation => lifetimeScope.Resolve<PartOfSpeechTranslationViewModel>())
                .Compile();
            _config.NewConfig<TranslationEntry, TranslationEntryViewModel>()
                .ConstructUsing(translationEntry => lifetimeScope.Resolve<TranslationEntryViewModel>())
                .Map(translationEntryViewModel => translationEntryViewModel.Text, translationEntry => translationEntry.Key.Text)
                .Map(translationEntryViewModel => translationEntryViewModel.Language, translationEntry => translationEntry.Key.SourceLanguage)
                .Map(translationEntryViewModel => translationEntryViewModel.TargetLanguage, translationEntry => translationEntry.Key.TargetLanguage)
                .AfterMapping(
                    async (translationEntry, translationEntryViewModel) =>
                    {
                        try
                        {
                            await translationEntryViewModel.ReloadTranslationsAsync()
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            lifetimeScope.Resolve<IMessageHub>()
                                .Publish(ex.ToMessage());
                        }
                    })
                .Compile();
            _config.NewConfig<TranslationInfo, TranslationDetailsViewModel>()
                .ConstructUsing(translationInfo => new TranslationDetailsViewModel(lifetimeScope.Resolve<TranslationResultViewModel>()))
                .Map(translationDetailsViewModel => translationDetailsViewModel.TranslationResult, translationInfo => translationInfo.TranslationDetails.TranslationResult)
                .Map(translationDetailsViewModel => translationDetailsViewModel.Id, translationInfo => translationInfo.TranslationDetails.Id)
                .Map(translationDetailsViewModel => translationDetailsViewModel.TranslationEntryId, translationInfo => translationInfo.TranslationDetails.TranslationEntryId)
                .AfterMapping(
                    (translationInfo, translationDetailsViewModel) =>
                    {
                        foreach (var partOfSpeechTranslationViewModel in translationDetailsViewModel.TranslationResult.PartOfSpeechTranslations)
                        {
                            partOfSpeechTranslationViewModel.Language = translationInfo.Key.SourceLanguage;
                            foreach (var translationVariantViewModel in partOfSpeechTranslationViewModel.TranslationVariants)
                            {
                                SetPriorityWordProperties(translationVariantViewModel, translationInfo.Key.TargetLanguage, translationDetailsViewModel.TranslationEntryId);
                                if (translationVariantViewModel.Synonyms != null)
                                    foreach (var synonym in translationVariantViewModel.Synonyms)
                                        SetPriorityWordProperties(synonym, translationInfo.Key.TargetLanguage, translationDetailsViewModel.TranslationEntryId);

                                if (translationVariantViewModel.Meanings == null)
                                    continue;

                                foreach (var meaning in translationVariantViewModel.Meanings)
                                    meaning.Language = translationInfo.Key.SourceLanguage;
                            }
                        }
                    })
                .Compile();
        }

        public TDestination Adapt<TDestination>(object source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Adapt<TDestination>(_config);
        }

        public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (Equals(source, default(TSource)))
                throw new ArgumentNullException(nameof(source));
            if (Equals(destination, default(TDestination)))
                throw new ArgumentNullException(nameof(destination));

            return source.Adapt(destination, _config);
        }

        private static void SetPriorityWordProperties([NotNull] PriorityWordViewModel priorityWordViewModel, [NotNull] string targetLanguage, [NotNull] object translationEntryId)
        {
            priorityWordViewModel.SetProperties(translationEntryId, targetLanguage);
        }
    }
}