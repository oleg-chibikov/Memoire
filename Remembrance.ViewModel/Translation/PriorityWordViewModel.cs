using System;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Scar.Common.Events;

namespace Remembrance.ViewModel.Translation
{
    public sealed class PriorityWordViewModelMainProperties
    {
        public PriorityWordViewModelMainProperties([NotNull] object translationEntryId, [NotNull] string partOfSpeechTranslationText, [NotNull] string language)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            PartOfSpeechTranslationText = partOfSpeechTranslationText ?? throw new ArgumentNullException(nameof(partOfSpeechTranslationText));
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }

        [NotNull]
        public object TranslationEntryId { get; }

        [NotNull]
        public string PartOfSpeechTranslationText { get; }

        [NotNull]
        public string Language { get; }
    }

    [AddINotifyPropertyChangedInterface]
    public class PriorityWordViewModel : WordViewModel
    {
        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly IMessageHub Messenger;

        public PriorityWordViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override bool CanEdit { get; } = true;

        [NotNull]
        public object TranslationEntryId { get; private set; }

        public event EventHandler<EventArgs<PriorityWordViewModelMainProperties>> TranslationEntryIdSet;

        public void SetProperties([NotNull] PriorityWordViewModelMainProperties priorityWordViewModelMainProperties)
        {
            //This function should be considered as a part of constructor. It cannot be a part of it because of using Mapster
            Language = priorityWordViewModelMainProperties?.Language ?? throw new ArgumentNullException(nameof(priorityWordViewModelMainProperties));
            TranslationEntryId = priorityWordViewModelMainProperties.TranslationEntryId;
            TranslationEntryIdSet?.Invoke(this, new EventArgs<PriorityWordViewModelMainProperties>(priorityWordViewModelMainProperties));
        }

        public void SetIsPriority(bool isPriority)
        {
            IsPriority = isPriority;
        }

        protected override void TogglePriority()
        {
            var isPriority = IsPriority;
            Logger.Info($"Changing priority for {this} to {!isPriority}");

            if (isPriority)
            {
                _wordPriorityRepository.Delete(new WordKey(TranslationEntryId, this));
            }
            else
            {
                _wordPriorityRepository.Insert(new WordPriority(new WordKey(TranslationEntryId, this)));
            }

            IsPriority = !isPriority;
            Messenger.Publish(this);
        }
    }
}