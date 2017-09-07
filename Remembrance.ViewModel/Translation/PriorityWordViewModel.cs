using System;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate;

namespace Remembrance.ViewModel.Translation
{
    [AddINotifyPropertyChangedInterface]
    [UsedImplicitly]
    public class PriorityWordViewModel : WordViewModel
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        public PriorityWordViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override bool CanEdit { get; } = true;

        [NotNull]
        public object TranslationEntryId { get; private set; }

        public void SetProperties([NotNull] object translationEntryId, [NotNull] string targetLanguage, [CanBeNull] bool? isPriority = null)
        {
            Language = targetLanguage ?? throw new ArgumentNullException(nameof(targetLanguage));
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            IsPriority = isPriority ?? _wordPriorityRepository.IsPriority(this, TranslationEntryId);
        }

        protected override void TogglePriority()
        {
            var isPriority = IsPriority;
            _logger.Info($"Changing priority for {this} to {!isPriority}");

            if (isPriority)
                _wordPriorityRepository.MarkNonPriority(this, TranslationEntryId);
            else
                _wordPriorityRepository.MarkPriority(this, TranslationEntryId);
            IsPriority = !isPriority;
            _messenger.Publish(this);
        }
    }
}