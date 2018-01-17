using System;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationVariantViewModel : PriorityWordViewModel
    {
        public TranslationVariantViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] ILifetimeScope lifetimeScope)
            : base(textToSpeechPlayer, messenger, wordsProcessor, logger, wordPriorityRepository)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));

            ImageViewModel = lifetimeScope.Resolve<ImageViewModel>(new TypedParameter(typeof(PriorityWordViewModel), this));
        }

        [CanBeNull]
        public PriorityWordViewModel[] Synonyms { get; set; }

        [CanBeNull]
        public WordViewModel[] Meanings { get; set; }

        [CanBeNull]
        public Example[] Examples { get; set; }

        [NotNull]
        public ImageViewModel ImageViewModel { get; }
    }
}