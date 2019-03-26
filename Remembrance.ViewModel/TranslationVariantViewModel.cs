using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.MVVM.Commands;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationVariantViewModel : PriorityWordViewModel
    {
        public TranslationVariantViewModel(
            [NotNull] TranslationEntry translationEntry,
            [NotNull] TranslationVariant translationVariant,
            [NotNull] string parentText,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messageHub,
            [NotNull] Func<Word, TranslationEntry, PriorityWordViewModel> priorityWordViewModelFactory,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] Func<WordKey, string, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ICommandManager commandManager)
            : base(translationEntry, translationVariant, textToSpeechPlayer, messageHub, translationEntryProcessor, logger, wordViewModelFactory, translationEntryRepository, commandManager)
        {
            _ = priorityWordViewModelFactory ?? throw new ArgumentNullException(nameof(priorityWordViewModelFactory));
            _ = wordImageViewerViewModelFactory ?? throw new ArgumentNullException(nameof(wordImageViewerViewModelFactory));
            Synonyms = translationVariant.Synonyms?.Select(synonym => priorityWordViewModelFactory(synonym, translationEntry)).ToArray();

            Meanings = translationVariant.Meanings?.Select(meaning => wordViewModelFactory(meaning, translationEntry.Id.SourceLanguage)).ToArray();

            Examples = translationVariant.Examples;

            WordImageViewerViewModel = wordImageViewerViewModelFactory(new WordKey(translationEntry.Id, Word), parentText);
        }

        [CanBeNull]
        public IReadOnlyCollection<Example>? Examples { get; }

        [CanBeNull]
        public IReadOnlyCollection<WordViewModel>? Meanings { get; }

        [CanBeNull]
        public IReadOnlyCollection<PriorityWordViewModel>? Synonyms { get; }

        [NotNull]
        public WordImageViewerViewModel WordImageViewerViewModel { get; }
    }
}