using System;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Shared;
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
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] ILifetimeScope lifetimeScope)
            : base(textToSpeechPlayer, messenger, translationEntryProcessor, logger, wordPriorityRepository)
        {
            if (lifetimeScope == null)
            {
                throw new ArgumentNullException(nameof(lifetimeScope));
            }

            WordImageViewerViewModel = lifetimeScope.Resolve<WordImageViewerViewModel>(new TypedParameter(typeof(PriorityWordViewModel), this));
        }

        [CanBeNull]
        public PriorityWordViewModel[] Synonyms { get; set; }

        [CanBeNull]
        public WordViewModel[] Meanings { get; set; }

        [CanBeNull]
        public Example[] Examples { get; set; }

        [NotNull]
        public WordImageViewerViewModel WordImageViewerViewModel { get; }
    }
}