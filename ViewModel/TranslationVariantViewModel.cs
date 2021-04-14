using System;
using System.Collections.Generic;
using System.Linq;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data.ExtendedTranslation;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationVariantViewModel : PriorityWordViewModel
    {
        public TranslationVariantViewModel(
            TranslationInfo translationInfo,
            TranslationVariant translationVariant,
            string parentText,
            ITextToSpeechPlayer textToSpeechPlayer,
            ITranslationEntryProcessor translationEntryProcessor,
            Func<Word, TranslationEntry, PriorityWordViewModel> priorityWordViewModelFactory,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            Func<WordKey, string, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            ILogger<PriorityWordViewModel> baseLogger,
            ITranslationEntryRepository translationEntryRepository,
            ICommandManager commandManager,
            ISharedSettingsRepository sharedSettingsRepository,
            IMessageHub messageHub) : base(
            translationInfo == null ? throw new ArgumentNullException(nameof(translationInfo)) : translationInfo.TranslationEntry,
            translationVariant,
            textToSpeechPlayer,
            translationEntryProcessor,
            baseLogger,
            wordViewModelFactory,
            translationEntryRepository,
            commandManager,
            sharedSettingsRepository,
            messageHub)
        {
            _ = parentText ?? throw new ArgumentNullException(nameof(parentText));
            _ = translationVariant ?? throw new ArgumentNullException(nameof(translationVariant));
            _ = priorityWordViewModelFactory ?? throw new ArgumentNullException(nameof(priorityWordViewModelFactory));
            _ = wordImageViewerViewModelFactory ?? throw new ArgumentNullException(nameof(wordImageViewerViewModelFactory));

            Synonyms = translationVariant.Synonyms?.Select(synonym => priorityWordViewModelFactory(synonym, translationInfo.TranslationEntry)).ToArray();
            Meanings = translationVariant.Meanings?.Select(meaning => wordViewModelFactory(meaning, translationInfo.TranslationEntryKey.SourceLanguage)).ToArray();
            Examples = translationVariant.Examples;
            WordImageViewerViewModel = wordImageViewerViewModelFactory(new WordKey(translationInfo.TranslationEntryKey, Word), parentText);
            ExtendedExamplesViewModel = new ExtendedExamplesViewModel(translationInfo, ti => GetExamples(ti, translationVariant), commandManager);
        }

        public ExtendedExamplesViewModel ExtendedExamplesViewModel { get; }

        public IReadOnlyCollection<Example>? Examples { get; }

        public IReadOnlyCollection<WordViewModel>? Meanings { get; }

        public IReadOnlyCollection<PriorityWordViewModel>? Synonyms { get; }

        public WordImageViewerViewModel WordImageViewerViewModel { get; }

        static IReadOnlyCollection<ExtendedPartOfSpeechTranslation>? GetExamples(TranslationInfo translationInfo, TranslationVariant translationVariant)
        {
            var translationVariantAndSynonymsHashSet = new HashSet<BaseWord>(translationVariant.GetTranslationVariantAndSynonyms());
            return translationInfo.TranslationDetails.ExtendedTranslationResult?.ExtendedPartOfSpeechTranslations.Where(
                    x => x.Translation.Text != null && translationVariantAndSynonymsHashSet.Contains(x.Translation))
                .ToArray();
        }
    }
}
