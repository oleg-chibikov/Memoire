using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LiteDB;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.DAL.Contracts.Model
{
    public sealed class TranslationEntry : Entity<int>
    {
        [NotNull]
        private LinkedListNode<RepeatType> _current;

        private DateTime _lastCardShowTime;
        private RepeatType _repeatType;

        [UsedImplicitly]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public TranslationEntry()
        {
        }

        public TranslationEntry([NotNull] TranslationEntryKey key, [NotNull] IList<PriorityWord> translations)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Translations = translations ?? throw new ArgumentNullException(nameof(translations));
            LastCardShowTime = DateTime.Now;
            RepeatType = RepeatTypeSettings.RepeatTypes.First.Value;
            _current = RepeatTypeSettings.RepeatTypes.First;
        }

        [NotNull]
        [UsedImplicitly]
        [BsonIndex(true)]
        public TranslationEntryKey Key { get; set; }

        [NotNull]
        [UsedImplicitly]
        public IList<PriorityWord> Translations { get; set; }

        [UsedImplicitly]
        public RepeatType RepeatType
        {
            get => _repeatType;
            set
            {
                _repeatType = value;
                _current = RepeatTypeSettings.RepeatTypes.Find(value) ?? RepeatTypeSettings.RepeatTypes.First;
                SetNextCardShowTime();
            }
        }

        [UsedImplicitly]
        public int ShowCount { get; set; }

        [UsedImplicitly]
        public DateTime LastCardShowTime
        {
            get => _lastCardShowTime;
            set
            {
                _lastCardShowTime = value;
                SetNextCardShowTime();
            }
        }

        [UsedImplicitly]
        [BsonIndex(false)]
        public DateTime NextCardShowTime { get; set; }

        public void DecreaseRepeatType()
        {
            var prev = _current.Previous;
            if (prev == null)
                return;

            RepeatType = prev.Value;
            _current = prev;
        }

        public void IncreaseRepeatType()
        {
            var next = _current.Next;
            if (next == null)
                return;

            RepeatType = next.Value;
            _current = next;
        }

        private void SetNextCardShowTime()
        {
            NextCardShowTime = _lastCardShowTime.Add(RepeatTypeSettings.RepeatTimes[_repeatType]);
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }
}