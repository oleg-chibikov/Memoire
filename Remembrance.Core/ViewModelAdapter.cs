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
                    translationEntryViewModel => new TranslationEntry
                    {
                        Key = new TranslationEntryKey(translationEntryViewModel.Text, translationEntryViewModel.Language, translationEntryViewModel.TargetLanguage)
                    })
                .Compile();
            TypeAdapterConfig<IWord, WordViewModel>.NewConfig()
                .ConstructUsing(word => lifetimeScope.Resolve<WordViewModel>())
                .Compile();
            TypeAdapterConfig<IWord, PriorityWordViewModel>.NewConfig()
                .ConstructUsing(word => lifetimeScope.Resolve<PriorityWordViewModel>())
                .Map(priorityWordViewModel => priorityWordViewModel.Text, word => word.Text) //This is unexpected, but still needed
                .Compile();
            TypeAdapterConfig<TranslationVariant, TranslationVariantViewModel>.NewConfig()
                .ConstructUsing(translationVariant => lifetimeScope.Resolve<TranslationVariantViewModel>())
                .Compile();
            TypeAdapterConfig<PartOfSpeechTranslation, PartOfSpeechTranslationViewModel>.NewConfig()
                .ConstructUsing(partOfSpeechTranslation => lifetimeScope.Resolve<PartOfSpeechTranslationViewModel>())
                .Compile();
            TypeAdapterConfig<TranslationEntry, TranslationEntryViewModel>.NewConfig()
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
            TypeAdapterConfig<TranslationInfo, TranslationDetailsViewModel>.NewConfig()
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