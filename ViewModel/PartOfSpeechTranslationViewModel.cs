using System;
using System.Collections.Generic;
using System.Linq;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel(
            TranslationInfo translationInfo,
            PartOfSpeechTranslation partOfSpeechTranslation,
            ITextToSpeechPlayer textToSpeechPlayer,
            Func<TranslationVariant, TranslationInfo, string, TranslationVariantViewModel> translationVariantViewModelFactory,
            ITranslationEntryProcessor translationEntryProcessor,
            ICommandManager commandManager,
            ISharedSettingsRepository sharedSettingsRepository,
            IMessageHub messageHub) : base(
            partOfSpeechTranslation,
            translationInfo == null ? throw new ArgumentNullException(nameof(translationInfo)) : translationInfo.TranslationEntryKey.SourceLanguage,
            textToSpeechPlayer,
            translationEntryProcessor,
            commandManager,
            sharedSettingsRepository,
            messageHub)
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
