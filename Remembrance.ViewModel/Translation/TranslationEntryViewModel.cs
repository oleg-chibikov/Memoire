using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate;
using Scar.Common.Notification;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel, INotificationSupressable
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        public TranslationEntryViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] ILog logger,
            [NotNull] TranslationEntryKey translationEntryKey,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ITranslationEntryRepository translationEntryRepository)
            : base(textToSpeechPlayer, translationEntryProcessor)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            CanLearnWord = false;
        }

        [DoNotNotify]
        public TranslationEntryKey Id { get; }

        public override string Text => Id.Text;

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

        public bool NotificationIsSupressed { get; set; }

        [NotNull]
        public NotificationSupresser SupressNotification()
        {
            return new NotificationSupresser(this);
        }

        public void ProcessPriority([NotNull] PriorityWordKey priorityWordKey)
        {
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            if (priorityWordKey.IsPriority)
            {
                ProcessPriority(priorityWordKey.WordKey);
            }
            else
            {
                ProcessNonPriority(priorityWordKey.WordKey);
            }
        }

        private void ProcessNonPriority([NotNull] WordKey wordKey)
        {
            var translations = Translations;
            _logger.TraceFormat("Removing non-priority word {1} from the list for {0}...", this, wordKey);
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];

                if (translation.Equals(wordKey.Word))
                {
                    _logger.TraceFormat("Removing {0} from the list...", wordKey);
                    _synchronizationContext.Send(x => Translations.RemoveAt(i--), null);
                }
            }

            if (!translations.Any())
            {
                _logger.Debug("No more translations left in the list. Restoring default...");
                var translationEntry = _translationEntryRepository.GetById(Id);
                ReloadTranslationsAsync(translationEntry).ConfigureAwait(false);
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
                        translation.SetIsPriority(true);
                        _logger.DebugFormat("{0} has been marked as priority", wordKey);
                    }
                    else
                    {
                        _logger.DebugFormat("Found {0} in the list but it is already priority", wordKey);
                    }

                    found = true;
                }

                if (!translation.IsPriority)
                {
                    _synchronizationContext.Send(x => Translations.RemoveAt(i--), null);
                }
            }

            if (!found)
            {
                _logger.TraceFormat("Not found {0} in the list. Adding...", wordKey);
                var copy = _viewModelAdapter.Adapt<PriorityWordViewModel>(wordKey.Word);
                copy.SetTranslationEntryKey(wordKey.TranslationEntryKey);
                var translationEntry = _translationEntryRepository.GetById(wordKey.TranslationEntryKey);
                copy.SetIsPriority(translationEntry.PriorityWords?.Contains(wordKey.Word) == true);
                _synchronizationContext.Send(x => Translations.Add(copy), null);
                _logger.DebugFormat("{0} has been added to the list", wordKey);
            }
        }

        public async Task ReloadTranslationsAsync([NotNull] TranslationEntry translationEntry)
        {
            if (translationEntry == null)
            {
                throw new ArgumentNullException(nameof(translationEntry));
            }

            var isPriority = translationEntry.PriorityWords?.Any() == true;
            var words = isPriority
                ? translationEntry.PriorityWords
                : (await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(Id, translationEntry.ManualTranslations, CancellationToken.None).ConfigureAwait(false)).TranslationResult.GetDefaultWords()
                .Cast<BaseWord>();
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