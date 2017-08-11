using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationEntry : Entity
    {
        [NotNull]
        private LinkedListNode<RepeatType> _current;

        private DateTime _lastCardShowTime;

        private RepeatType _repeatType;

        [UsedImplicitly]
        public TranslationEntry()
        {
        }

        public TranslationEntry([NotNull] TranslationEntryKey key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            LastCardShowTime = DateTime.Now;
            RepeatType = RepeatTypeSettings.RepeatTypes.First.Value;
            _current = RepeatTypeSettings.RepeatTypes.First;
        }

        [NotNull]
        [UsedImplicitly]
        public TranslationEntryKey Key { get; set; }

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