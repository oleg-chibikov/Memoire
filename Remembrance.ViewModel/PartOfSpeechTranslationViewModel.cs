using System;
using System.Collections.Generic;
using System.Linq;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.MVVM.Commands;

namespace Remembrance.ViewModel
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel(
            TranslationEntry translationEntry,
            PartOfSpeechTranslation partOfSpeechTranslation,
            ITextToSpeechPlayer textToSpeechPlayer,
            Func<TranslationVariant, TranslationEntry, string, TranslationVariantViewModel> translationVariantViewModelFactory,
            ITranslationEntryProcessor translationEntryProcessor,
            ICommandManager commandManager)
            : base(partOfSpeechTranslation, translationEntry.Id.SourceLanguage, textToSpeechPlayer, translationEntryProcessor, commandManager)
        {
            _ = partOfSpeechTranslation ?? throw new ArgumentNullException(nameof(partOfSpeechTranslation));
            _ = translationVariantViewModelFactory ?? throw new ArgumentNullException(nameof(translationVariantViewModelFactory));
            Transcription = partOfSpeechTranslation.Transcription;
            TranslationVariants = partOfSpeechTranslation.TranslationVariants
                .Select(translationVariant => translationVariantViewModelFactory(translationVariant, translationEntry, Word.Text))
                .ToArray();
            CanLearnWord = false;
        }

        public string? Transcription { get; }

        public IReadOnlyCollection<TranslationVariantViewModel> TranslationVariants { get; }
    }
}