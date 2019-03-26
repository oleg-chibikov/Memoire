using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel : BaseViewModel
    {
        public TranslationResultViewModel(
            [NotNull] TranslationResult translationResult,
            [NotNull] TranslationEntry translationEntry,
            [NotNull] Func<PartOfSpeechTranslation, TranslationEntry, PartOfSpeechTranslationViewModel> partOfSpeechTranslationViewModelFactory,
            [NotNull] ICommandManager commandManager)
            : base(commandManager)
        {
            _ = partOfSpeechTranslationViewModelFactory ?? throw new ArgumentNullException(nameof(partOfSpeechTranslationViewModelFactory));
            _ = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
            PartOfSpeechTranslations = translationResult.PartOfSpeechTranslations
                .Select(partOfSpeechTranslation => partOfSpeechTranslationViewModelFactory(partOfSpeechTranslation, translationEntry))
                .ToArray();
        }

        [NotNull]
        public IReadOnlyCollection<PartOfSpeechTranslationViewModel> PartOfSpeechTranslations { get; }
    }
}