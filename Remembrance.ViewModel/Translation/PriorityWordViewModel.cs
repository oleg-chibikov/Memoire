using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ITranslationEntryRepository _translationEntryRepository;

        public PriorityWordViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryRepository translationEntryRepository)
            : base(textToSpeechPlayer, translationEntryProcessor)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordKey>(OnPriorityChanged));
        }

        [DoNotNotify]
        public override bool CanEdit { get; } = true;

        [NotNull]
        private WordKey _wordKey;

        [NotNull]
        private TranslationEntryKey _translationEntryKey;

        [NotNull][DoNotNotify]
        public override string Language => _translationEntryKey.TargetLanguage;

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
            if (!wordKey.Equals(_wordKey))
            {
                return;
            }

            IsPriority = priorityWordKey.IsPriority;
            _logger.InfoFormat("Priority changed for {0}", priorityWordKey);
        }

        public event EventHandler<EventArgs<WordKey>> WordKeySet;
        public event EventHandler<EventArgs<string>> ParentTextSet;

        public void SetTranslationEntryKey([NotNull] TranslationEntryKey translationEntryKey)
        {
            if (translationEntryKey == null)
            {
                throw new ArgumentNullException(nameof(translationEntryKey));
            }

            //This function should be considered as a part of constructor. It cannot be a part of it because of using Mapster
            _translationEntryKey = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            _wordKey = new WordKey(translationEntryKey, new BaseWord(this));
            WordKeySet?.Invoke(this, new EventArgs<WordKey>(_wordKey));
        }

        public void SetIsPriority(bool isPriority)
        {
            //This function should be considered as a part of constructor. It cannot be a part of it because of using Mapster
            IsPriority = isPriority;
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
            var isPriority = !IsPriority;
            _logger.TraceFormat("Changing priority for {0} to {1}...", _wordKey.Word, isPriority);
            var translationEntry = _translationEntryRepository.GetById(_translationEntryKey);
            if (isPriority)
            {
                if (translationEntry.PriorityWords == null)
                {
                    translationEntry.PriorityWords = new HashSet<BaseWord>{_wordKey.Word};
                }
                else
                {
                    translationEntry.PriorityWords.Add(_wordKey.Word);
                }
            }
            else
            {
                if (translationEntry.PriorityWords == null)
                {
                    throw new InvalidOperationException("PriorityWords should not be null when deleting");
                }
                translationEntry.PriorityWords.Remove(_wordKey.Word);
                if (!translationEntry.PriorityWords.Any())
                {
                    translationEntry.PriorityWords = null;
                }
            }

            _translationEntryRepository.Update(translationEntry);

            var priorityWordKey = new PriorityWordKey(isPriority, _wordKey);
            _messenger.Publish(priorityWordKey);
        }
    }
}