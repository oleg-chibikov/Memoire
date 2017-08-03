using System;
using System.Collections.ObjectModel;
using System.Threading;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common;
using Scar.Common.Notification;

namespace Remembrance.ViewModel.Translation
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel, INotificationSupressable
    {
        private string _text;

        [UsedImplicitly]
        public TranslationEntryViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            CanLearnWord = false;
        }

        [UsedImplicitly]
        [DoNotNotify]
        public object Id { get; set; }

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
                    _text = newValue;
                }
            }
        }

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations { get; set; }

        [UsedImplicitly]
        public int ShowCount { get; set; }

        [NotNull]
        [UsedImplicitly]
        public override string Language { get; set; }

        [NotNull]
        [UsedImplicitly]
        public string TargetLanguage { get; set; }

        [UsedImplicitly]
        public RepeatType RepeatType { get; set; }

        [UsedImplicitly]
        public DateTime LastCardShowTime { get; set; }

        [UsedImplicitly]
        public DateTime NextCardShowTime { get; set; }

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