using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Scar.Common.Notification;

namespace Remembrance.ViewModel.Translation
{
    //TODO: Subscribe events in their own threads everywhere.
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel, INotificationSupressable, IDisposable
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        public TranslationEntryViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] TranslationEntryKey translationEntryKey,
            [CanBeNull] HashSet<BaseWord> priorityWords,
            [NotNull] SynchronizationContext synchronizationContext)
            : base(textToSpeechPlayer, translationEntryProcessor)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            PriorityWords = priorityWords;
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            CanLearnWord = false;
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordKey>(OnPriorityChangedAsync));

            ReloadTranslationsAsync().ConfigureAwait(false);
        }

        [DoNotNotify]
        public TranslationEntryKey Id { get; }

        public override string Text => Id.Text;

        [CanBeNull]
        public ManualTranslation[] ManualTranslations { get; set; }

        [CanBeNull]
        public HashSet<BaseWord> PriorityWords { get; }

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations { get; private set; }

        public int ShowCount { get; set; }

        [NotNull]
        public override string Language => Id.SourceLanguage;

        [NotNull]
        public string TargetLanguage => Id.TargetLanguage;

        public RepeatType RepeatType { get; set; }

        public DateTime LastCardShowTime { get; set; }

        public DateTime NextCardShowTime { get; set; }

        public bool IsFavorited { get; set; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messenger.UnSubscribe(subscriptionToken);
            }
        }

        public bool NotificationIsSupressed { get; set; }

        [NotNull]
        public NotificationSupresser SupressNotification()
        {
            return new NotificationSupresser(this);
        }

        private async Task ProcessNonPriorityAsync([NotNull] WordKey wordKey)
        {
            var translations = Translations;
            _logger.TraceFormat("Removing non-priority word {1} from the list for {0}...", this, wordKey);
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];

                if (translation.Equals(wordKey.Word))
                {
                    _logger.TraceFormat("Removing {0} from the list...", wordKey);
                    translations.RemoveAt(i--);
                }
            }

            if (!translations.Any())
            {
                _logger.Trace("No more translations left in the list. Restoring default...");
                await ReloadTranslationsAsync().ConfigureAwait(false);
            }
        }

        private void ProcessPriority([NotNull] WordKey wordKey)
        {
            _logger.TraceFormat("Removing all non-priority translations for {0} except {1}...", this, wordKey);
            var found = false;
            for (var i = 0; i < Translations.Count; i++)
            {
                var translation = Translations[i];
                if (translation.Equals(wordKey.Word))
                {
                    if (!translation.IsPriority)
                    {
                        _logger.TraceFormat("Found {0} in the list. Marking as priority...", wordKey);
                        translation.IsPriority = true;
                    }
                    else
                    {
                        _logger.TraceFormat("Found {0} in the list but it is already priority", wordKey);
                    }

                    found = true;
                }

                if (!translation.IsPriority)
                {
                    _synchronizationContext.Send(x=> Translations.RemoveAt(i),null);
                    i--;
                }
            }

            if (!found)
            {
                _logger.TraceFormat("Not found {0} in the list. Adding...", wordKey);
                var copy = _viewModelAdapter.Adapt<PriorityWordViewModel>(wordKey.Word);
                copy.SetTranslationEntryKey(wordKey);
                copy.SetIsPriority(PriorityWords?.Contains(wordKey.Word) == true);
                _synchronizationContext.Send(x => Translations.Add(copy), null);
            }
        }

        private async void OnPriorityChangedAsync([NotNull] PriorityWordKey priorityWordKey)
        {
            _logger.TraceFormat("Changing priority for {0} in the list...", priorityWordKey);
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            var wordKey = priorityWordKey.WordKey;
            if (!wordKey.Equals(Id))
            {
                return;
            }

            if (priorityWordKey.IsPriority)
            {
                ProcessPriority(wordKey);
            }
            else
            {
                await ProcessNonPriorityAsync(wordKey).ConfigureAwait(false);
            }
        }

        public async Task ReloadTranslationsAsync()
        {
            var isPriority = PriorityWords?.Any() == true;
            var words = isPriority
                ? PriorityWords
                : (await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(Id, ManualTranslations, CancellationToken.None).ConfigureAwait(false)).TranslationResult.GetDefaultWords().Cast<BaseWord>();
            var translations = _viewModelAdapter.Adapt<PriorityWordViewModel[]>(words);
            foreach (var translation in translations)
            {
                translation.SetTranslationEntryKey(Id);
                translation.SetIsPriority(isPriority);
            }

            Translations = new ObservableCollection<PriorityWordViewModel>(translations);
        }

        public override string ToString()
        {
            return $"{Id}";
        }
    }
}