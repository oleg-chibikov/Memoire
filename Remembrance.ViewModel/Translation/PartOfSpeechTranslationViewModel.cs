using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel(
            [NotNull] TranslationEntry translationEntry,
            [NotNull] PartOfSpeechTranslation partOfSpeechTranslation,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor)
            : base(partOfSpeechTranslation, translationEntry.Id.SourceLanguage, lifetimeScope, textToSpeechPlayer, translationEntryProcessor)
        {
            if (partOfSpeechTranslation == null)
            {
                throw new ArgumentNullException(nameof(partOfSpeechTranslation));
            }

            Transcription = partOfSpeechTranslation.Transcription;
            TranslationVariants = partOfSpeechTranslation.TranslationVariants.Select(
                    translationVariant => lifetimeScope.Resolve<TranslationVariantViewModel>(
                        new TypedParameter(typeof(TranslationVariant), translationVariant),
                        new TypedParameter(typeof(TranslationEntry), translationEntry),
                        new TypedParameter(typeof(string), Text)))
                .ToArray();
            CanLearnWord = false;
        }

        [CanBeNull]
        public string Transcription { get; }

        [NotNull]
        public ICollection<TranslationVariantViewModel> TranslationVariants { get; }
    }
}