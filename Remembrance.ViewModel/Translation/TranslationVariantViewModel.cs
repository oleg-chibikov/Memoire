using System.Collections.Generic;
using System.Linq;
using Autofac;
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
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryRepository translationEntryRepository)
            : base(translationEntry, translationVariant, lifetimeScope, textToSpeechPlayer, messageHub, translationEntryProcessor, logger, translationEntryRepository)
        {
            Synonyms = translationVariant.Synonyms
                ?.Select(synonym => lifetimeScope.Resolve<PriorityWordViewModel>(new TypedParameter(typeof(Word), synonym), new TypedParameter(typeof(TranslationEntry), translationEntry)))
                .ToArray();

            Meanings = translationVariant.Meanings
                ?.Select(meaning => lifetimeScope.Resolve<WordViewModel>(new TypedParameter(typeof(Word), meaning), new TypedParameter(typeof(string), translationEntry.Id.SourceLanguage)))
                .ToArray();

            Examples = translationVariant.Examples;

            WordImageViewerViewModel = lifetimeScope.Resolve<WordImageViewerViewModel>(
                new TypedParameter(typeof(WordKey), new WordKey(translationEntry.Id, new BaseWord(this))),
                new TypedParameter(typeof(string), parentText));
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