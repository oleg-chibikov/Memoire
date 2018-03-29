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
using Remembrance.ViewModel.Card;

namespace Remembrance.ViewModel.Translation
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
            [NotNull] Func<WordKey, string, WordImageViewerViewModel> wordImageViewerViewModel,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryRepository translationEntryRepository)
            : base(translationEntry, translationVariant, textToSpeechPlayer, messageHub, translationEntryProcessor, logger, wordViewModelFactory, translationEntryRepository)
        {
            if (priorityWordViewModelFactory == null)
            {
                throw new ArgumentNullException(nameof(priorityWordViewModelFactory));
            }

            if (wordImageViewerViewModel == null)
            {
                throw new ArgumentNullException(nameof(wordImageViewerViewModel));
            }

            Synonyms = translationVariant.Synonyms
                ?.Select(synonym => priorityWordViewModelFactory(synonym, translationEntry))
                .ToArray();

            Meanings = translationVariant.Meanings
                ?.Select(meaning => wordViewModelFactory(meaning, translationEntry.Id.SourceLanguage))
                .ToArray();

            Examples = translationVariant.Examples;

            WordImageViewerViewModel = wordImageViewerViewModel(new WordKey(translationEntry.Id, new BaseWord(this)), parentText);
        }

        [CanBeNull]
        public ICollection<Example> Examples { get; }

        [CanBeNull]
        public ICollection<WordViewModel> Meanings { get; }

        [CanBeNull]
        public ICollection<PriorityWordViewModel> Synonyms { get; }

        [NotNull]
        public WordImageViewerViewModel WordImageViewerViewModel { get; }
    }
}