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
        private string language;

        private DateTime lastCardShowTime;

        private DateTime nextCardShowTime;

        private RepeatType repeatType;

        private int showCount;

        private string targetLanguage;

        private string text;

        private ObservableCollection<PriorityWordViewModel> translations;

        [UsedImplicitly]
        public TranslationEntryViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor) : base(textToSpeechPlayer, wordsProcessor)
        {
        }

        [UsedImplicitly]
        public int Id { get; set; }

        [UsedImplicitly]
        public override string Text
        {
            get { return text ?? string.Empty; }
            set
            {
                var newValue = value.Capitalize();
                if (newValue == text)
                    return;

                //For the new item this event should not be fired
                if (text != null && !NotificationIsSupressed)
                {
                    var handler = Volatile.Read(ref TextChanged);
                    handler?.Invoke(this, new TextChangedEventArgs(newValue, text));
                }
                else
                {
                    Set(() => Text, ref text, newValue);
                }
            }
        }

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations
        {
            get { return translations; }
            set { Set(() => Translations, ref translations, value); }
        }

        [UsedImplicitly]
        public int ShowCount
        {
            get { return showCount; }
            set { Set(() => ShowCount, ref showCount, value); }
        }

        [NotNull, UsedImplicitly]
        public override string Language
        {
            get { return language ?? string.Empty; }
            set { Set(() => Language, ref language, value); }
        }

        [NotNull, UsedImplicitly]
        public string TargetLanguage
        {
            get { return targetLanguage ?? string.Empty; }
            set { Set(() => TargetLanguage, ref targetLanguage, value); }
        }

        [UsedImplicitly]
        public RepeatType RepeatType
        {
            get { return repeatType; }
            set { Set(() => RepeatType, ref repeatType, value); }
        }

        [UsedImplicitly]
        public DateTime LastCardShowTime
        {
            get { return lastCardShowTime; }
            set { Set(() => LastCardShowTime, ref lastCardShowTime, value); }
        }

        [UsedImplicitly]
        public DateTime NextCardShowTime
        {
            get { return nextCardShowTime; }
            set { Set(() => NextCardShowTime, ref nextCardShowTime, value); }
        }

        public event TextChangedEventHandler TextChanged;

        public override string ToString()
        {
            return $"{Text} [{Language}->{TargetLanguage}]";
        }

        public bool NotificationIsSupressed { get; set; }

        public NotificationSupresser SupressNotification()
        {
            return new NotificationSupresser(this);
        }
    }
}