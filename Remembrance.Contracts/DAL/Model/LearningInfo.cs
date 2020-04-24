using System;
using System.Collections.Generic;
using Remembrance.Contracts.Classification.Data;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class LearningInfo : TrackedEntity<TranslationEntryKey>
    {
        LinkedListNode<RepeatType> _current;

        DateTime _lastCardShowTime;

        RepeatType _repeatType;

        public LearningInfo()
        {
            LastCardShowTime = DateTime.Now;
            RepeatType = RepeatTypeSettings.RepeatTypes.First.Value;
            _current = RepeatTypeSettings.RepeatTypes.First;
        }

        public bool IsFavorited { get; set; }

        public DateTime LastCardShowTime
        {
            get => _lastCardShowTime;
            set
            {
                _lastCardShowTime = value;
                SetNextCardShowTime();
            }
        }

        public DateTime NextCardShowTime { get; set; }

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

        public int ShowCount { get; set; }

        public IReadOnlyCollection<ClassificationCategory>? ClassificationCategories { get; set; }

        public override string ToString()
        {
            return $"Learning info for {Id}";
        }

        public void DecreaseRepeatType()
        {
            var prev = _current.Previous;
            if (prev == null)
            {
                return;
            }

            RepeatType = prev.Value;
            _current = prev;
        }

        public void IncreaseRepeatType()
        {
            var next = _current.Next;
            if (next == null)
            {
                return;
            }

            RepeatType = next.Value;
            _current = next;
        }

        void SetNextCardShowTime()
        {
            NextCardShowTime = _lastCardShowTime.Add(RepeatTypeSettings.RepeatTimes[_repeatType]);
        }
    }
}
