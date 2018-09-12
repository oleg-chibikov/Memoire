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
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PriorityWordViewModel : WordViewModel
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly TranslationEntry _translationEntry;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly WordKey _wordKey;

        public PriorityWordViewModel(
            [NotNull] TranslationEntry translationEntry,
            [NotNull] Word word,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messageHub,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] ITranslationEntryRepository translationEntryRepository)
            : base(word, translationEntry.Id.TargetLanguage, textToSpeechPlayer, translationEntryProcessor)
        {
            _ = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            _ = wordViewModelFactory ?? throw new ArgumentNullException(nameof(wordViewModelFactory));
            IsPriority = translationEntry.PriorityWords?.Contains(word) ?? false;
            _translationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _wordKey = new WordKey(translationEntry.Id, word);
        }

        [DoNotNotify]
        public override bool CanEdit { get; } = true;

        [NotNull]
        [DoNotNotify]
        public override string Language => _translationEntry.Id.TargetLanguage;

        public void SetIsPriority(bool isPriority)
        {
            IsPriority = isPriority;
        }

        protected override void TogglePriority()
        {
            var isPriority = !IsPriority;
            _logger.TraceFormat("Changing priority for {0} to {1}...", _wordKey, isPriority);
            if (isPriority)
            {
                if (_translationEntry.PriorityWords == null)
                {
                    _translationEntry.PriorityWords = new HashSet<BaseWord>
                    {
                        _wordKey.Word
                    };
                }
                else
                {
                    _translationEntry.PriorityWords.Add(_wordKey.Word);
                }
            }
            else
            {
                if (_translationEntry.PriorityWords == null)
                {
                    throw new InvalidOperationException("PriorityWords should not be null when deleting");
                }

                _translationEntry.PriorityWords.Remove(_wordKey.Word);
                if (!_translationEntry.PriorityWords.Any())
                {
                    _translationEntry.PriorityWords = null;
                }
            }

            _translationEntryRepository.Update(_translationEntry);
            IsPriority = isPriority;
            _logger.InfoFormat("Priority has been changed for {0}", _wordKey);

            var priorityWordKey = new PriorityWordKey(isPriority, _wordKey);
            _messageHub.Publish(priorityWordKey);
        }
    }
}