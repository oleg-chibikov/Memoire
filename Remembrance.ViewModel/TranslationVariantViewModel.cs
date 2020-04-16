using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.MVVM.Commands;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationVariantViewModel : PriorityWordViewModel
    {
        public TranslationVariantViewModel(
            TranslationEntry translationEntry,
            TranslationVariant translationVariant,
            string parentText,
            ITextToSpeechPlayer textToSpeechPlayer,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messageHub,
            Func<Word, TranslationEntry, PriorityWordViewModel> priorityWordViewModelFactory,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            Func<WordKey, string, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            ILog logger,
            ITranslationEntryRepository translationEntryRepository,
            ICommandManager commandManager) : base(
            translationEntry,
            translationVariant,
            textToSpeechPlayer,
            messageHub,
            translationEntryProcessor,
            logger,
            wordViewModelFactory,
            translationEntryRepository,
            commandManager)
        {
            _ = parentText ?? throw new ArgumentNullException(nameof(parentText));
            _ = translationVariant ?? throw new ArgumentNullException(nameof(translationVariant));
            _ = priorityWordViewModelFactory ?? throw new ArgumentNullException(nameof(priorityWordViewModelFactory));
            _ = wordImageViewerViewModelFactory ?? throw new ArgumentNullException(nameof(wordImageViewerViewModelFactory));

            Synonyms = translationVariant.Synonyms?.Select(synonym => priorityWordViewModelFactory(synonym, translationEntry)).ToArray();
            Meanings = translationVariant.Meanings?.Select(meaning => wordViewModelFactory(meaning, translationEntry.Id.SourceLanguage)).ToArray();
            Examples = translationVariant.Examples;
            WordImageViewerViewModel = wordImageViewerViewModelFactory(new WordKey(translationEntry.Id, Word), parentText);
        }

        public IReadOnlyCollection<Example>? Examples { get; }

        public IReadOnlyCollection<WordViewModel>? Meanings { get; }

        public IReadOnlyCollection<PriorityWordViewModel>? Synonyms { get; }

        public WordImageViewerViewModel WordImageViewerViewModel { get; }
    }
}
