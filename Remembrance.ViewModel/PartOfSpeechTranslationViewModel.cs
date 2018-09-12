using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel(
            [NotNull] TranslationEntry translationEntry,
            [NotNull] PartOfSpeechTranslation partOfSpeechTranslation,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] Func<TranslationVariant, TranslationEntry, string, TranslationVariantViewModel> translationVariantViewModelFactory,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor)
            : base(partOfSpeechTranslation, translationEntry.Id.SourceLanguage, textToSpeechPlayer, translationEntryProcessor)
        {
            _ = partOfSpeechTranslation ?? throw new ArgumentNullException(nameof(partOfSpeechTranslation));
            _ = translationVariantViewModelFactory ?? throw new ArgumentNullException(nameof(translationVariantViewModelFactory));
            Transcription = partOfSpeechTranslation.Transcription;
            TranslationVariants = partOfSpeechTranslation.TranslationVariants
                .Select(translationVariant => translationVariantViewModelFactory(translationVariant, translationEntry, Word.Text))
                .ToArray();
            CanLearnWord = false;
        }

        [CanBeNull]
        public string Transcription { get; }

        [NotNull]
        public IReadOnlyCollection<TranslationVariantViewModel> TranslationVariants { get; }
    }
}