using System;
using System.Collections.ObjectModel;
using System.Threading;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common;
using Scar.Common.Notification;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public sealed class TranslationEntryViewModel : WordViewModel, INotificationSupressable
    {
        private string _language;

        private DateTime _lastCardShowTime;

        private DateTime _nextCardShowTime;

        private RepeatType _repeatType;

        private int _showCount;

        private string _targetLanguage;

        private string _text;

        private ObservableCollection<PriorityWordViewModel> _translations;

        [UsedImplicitly]
        public TranslationEntryViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor)
            : base(textToSpeechPlayer, wordsProcessor)
        {
        }

        [UsedImplicitly]
        public int Id { get; set; }

        [UsedImplicitly]
        public override string Text
        {
            get => _text ?? string.Empty;
            set
            {
                var newValue = value.Capitalize();
                if (newValue == _text)
                    return;

                //For the new item this event should not be fired
                if (_text != null && !NotificationIsSupressed)
                {
                    var handler = Volatile.Read(ref TextChanged);
                    handler?.Invoke(this, new TextChangedEventArgs(newValue, _text));
                }
                else
                {
                    Set(() => Text, ref _text, newValue);
                }
            }
        }

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations
        {
            get => _translations;
            set { Set(() => Translations, ref _translations, value); }
        }

        [UsedImplicitly]
        public int ShowCount
        {
            get => _showCount;
            set { Set(() => ShowCount, ref _showCount, value); }
        }

        [NotNull]
        [UsedImplicitly]
        public override string Language
        {
            get => _language ?? string.Empty;
            set { Set(() => Language, ref _language, value); }
        }

        [NotNull]
        [UsedImplicitly]
        public string TargetLanguage
        {
            get => _targetLanguage ?? string.Empty;
            set { Set(() => TargetLanguage, ref _targetLanguage, value); }
        }

        [UsedImplicitly]
        public RepeatType RepeatType
        {
            get => _repeatType;
            set { Set(() => RepeatType, ref _repeatType, value); }
        }

        [UsedImplicitly]
        public DateTime LastCardShowTime
        {
            get => _lastCardShowTime;
            set { Set(() => LastCardShowTime, ref _lastCardShowTime, value); }
        }

        [UsedImplicitly]
        public DateTime NextCardShowTime
        {
            get => _nextCardShowTime;
            set { Set(() => NextCardShowTime, ref _nextCardShowTime, value); }
        }

        public bool NotificationIsSupressed { get; set; }

        [NotNull]
        public NotificationSupresser SupressNotification()
        {
            return new NotificationSupresser(this);
        }

        public event TextChangedEventHandler TextChanged;

        public override string ToString()
        {
            return $"{Text} [{Language}->{TargetLanguage}]";
        }
    }
}