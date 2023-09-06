using System;
using System.Collections.Generic;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PriorityWordViewModel : WordViewModel
    {
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly TranslationEntry _translationEntry;
        readonly ITranslationEntryRepository _translationEntryRepository;
        readonly WordKey _wordKey;

        public PriorityWordViewModel(
            TranslationEntry translationEntry,
            Word word,
            ITextToSpeechPlayerWrapper textToSpeechPlayerWrapper,
            ITranslationEntryProcessor translationEntryProcessor,
            ILogger<PriorityWordViewModel> logger,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            ITranslationEntryRepository translationEntryRepository,
            ICommandManager commandManager,
            IMessageHub messageHub) : base(
            word,
            translationEntry?.Id.TargetLanguage ?? throw new ArgumentNullException(nameof(translationEntry)),
            textToSpeechPlayerWrapper,
            translationEntryProcessor,
            commandManager)
        {
            _ = wordViewModelFactory ?? throw new ArgumentNullException(nameof(wordViewModelFactory));
            IsPriority = translationEntry.PriorityWords?.Contains(word) ?? false;
            _translationEntry = translationEntry;
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _wordKey = new WordKey(translationEntry.Id, word);
        }

        [DoNotNotify]
        public override bool CanEdit { get; } = true;

        [DoNotNotify]
        public override string Language => _translationEntry.Id.TargetLanguage;

        public void SetIsPriority(bool isPriority)
        {
            IsPriority = isPriority;
        }

        protected override void TogglePriority()
        {
            var isPriority = !IsPriority;
            _logger.LogTrace("Changing priority for {WordKey} to {IsPriority}...", _wordKey, isPriority);
            if (isPriority)
            {
                if (_translationEntry.PriorityWords == null)
                {
                    _translationEntry.PriorityWords = new HashSet<BaseWord> { _wordKey.Word };
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
                if (!(_translationEntry.PriorityWords.Count > 0))
                {
                    _translationEntry.PriorityWords = null;
                }
            }

            _translationEntryRepository.Update(_translationEntry);
            IsPriority = isPriority;
            _logger.LogInformation("Priority has been changed for {WordKey}", _wordKey);

            var priorityWordKey = new PriorityWordKey(isPriority, _wordKey);
            _messageHub.Publish(priorityWordKey);
        }
    }
}
