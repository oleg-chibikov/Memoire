using System;
using System.Collections.Generic;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate;
using Scar.Common.Events;

namespace Remembrance.ViewModel.Translation
{
    [AddINotifyPropertyChangedInterface]
    public class PriorityWordViewModel : WordViewModel, IDisposable
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        public PriorityWordViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
            : base(textToSpeechPlayer, translationEntryProcessor)
        {
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordKey>(OnPriorityChanged));
        }

        public override bool CanEdit { get; } = true;

        [NotNull]
        public WordKey WordKey { get; private set; }

        [NotNull]
        public override string Language => WordKey.TargetLanguage;

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messenger.UnSubscribe(subscriptionToken);
            }
        }

        private void OnPriorityChanged([NotNull] PriorityWordKey priorityWordKey)
        {
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            var wordKey = priorityWordKey.WordKey;
            if (!wordKey.Equals(WordKey))
            {
                return;
            }

            IsPriority = priorityWordKey.IsPriority;
            _logger.InfoFormat("Priority changed for {0}", priorityWordKey);
        }

        public event EventHandler<EventArgs<TranslationEntryKey>> TranslationEntryKeySet;
        public event EventHandler<EventArgs<string>> ParentTextSet;

        public void SetTranslationEntryKey([NotNull] TranslationEntryKey translationEntryKey)
        {
            if (translationEntryKey == null)
            {
                throw new ArgumentNullException(nameof(translationEntryKey));
            }

            //This function should be considered as a part of constructor. It cannot be a part of it because of using Mapster
            WordKey = new WordKey(translationEntryKey, this) ?? throw new ArgumentNullException(nameof(translationEntryKey));
            TranslationEntryKeySet?.Invoke(this, new EventArgs<TranslationEntryKey>(translationEntryKey));
            IsPriority = _wordPriorityRepository.Check(new WordKey(translationEntryKey, this));
        }

        public void SetParentText([NotNull] string parentText)
        {
            if (parentText == null)
            {
                throw new ArgumentNullException(nameof(parentText));
            }

            ParentTextSet?.Invoke(this, new EventArgs<string>(parentText));
        }

        protected override void TogglePriority()
        {
            var isPriority = IsPriority;
            _logger.TraceFormat("Changing priority for {0} to {1}...", this, !isPriority);

            if (isPriority)
            {
                _wordPriorityRepository.Delete(WordKey);
            }
            else
            {
                _wordPriorityRepository.Insert(new WordPriority(WordKey));
            }

            var priorityWordKey = new PriorityWordKey(!isPriority, WordKey);
            _messenger.Publish(priorityWordKey);
        }
    }
}