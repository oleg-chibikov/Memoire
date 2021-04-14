using System;
using System.Collections.Generic;
using System.Linq;
using Mémoire.Contracts;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel(
            TranslationInfo translationInfo,
            PartOfSpeechTranslation partOfSpeechTranslation,
            ITextToSpeechPlayerWrapper textToSpeechPlayerWrapper,
            Func<TranslationVariant, TranslationInfo, string, TranslationVariantViewModel> translationVariantViewModelFactory,
            ITranslationEntryProcessor translationEntryProcessor,
            ICommandManager commandManager) : base(
            partOfSpeechTranslation,
            translationInfo == null ? throw new ArgumentNullException(nameof(translationInfo)) : translationInfo.TranslationEntryKey.SourceLanguage,
            textToSpeechPlayerWrapper,
            translationEntryProcessor,
            commandManager)
        {
            _ = partOfSpeechTranslation ?? throw new ArgumentNullException(nameof(partOfSpeechTranslation));
            _ = translationVariantViewModelFactory ?? throw new ArgumentNullException(nameof(translationVariantViewModelFactory));
            Transcription = partOfSpeechTranslation.Transcription;
            TranslationVariants = partOfSpeechTranslation.TranslationVariants.Select(translationVariant => translationVariantViewModelFactory(translationVariant, translationInfo, Word.Text))
                .ToArray();
            CanLearnWord = false;
        }

        public string? Transcription { get; }

        public IReadOnlyCollection<TranslationVariantViewModel> TranslationVariants { get; }
    }
}
