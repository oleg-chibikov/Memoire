using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LiteDB;
using Remembrance.Translate.Contracts.Data.WordsTranslator;

namespace Remembrance.DAL.Contracts.Model
{
    public sealed class TranslationEntry : Entity
    {
        [NotNull]
        private LinkedListNode<RepeatType> current;

        private DateTime lastCardShowTime;
        private RepeatType repeatType;

        [UsedImplicitly]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public TranslationEntry()
        {
        }

        public TranslationEntry([NotNull] TranslationEntryKey key, [NotNull] IList<PriorityWord> translations)
        {
            LastCardShowTime = DateTime.Now;
            RepeatType = RepeatTypeSettings.RepeatTypes.First.Value;
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Translations = translations ?? throw new ArgumentNullException(nameof(translations));
            current = RepeatTypeSettings.RepeatTypes.First;
        }

        [NotNull, UsedImplicitly, BsonIndex(true)]
        public TranslationEntryKey Key { get; set; }

        [NotNull, UsedImplicitly]
        public IList<PriorityWord> Translations { get; set; }

        [UsedImplicitly]
        public RepeatType RepeatType
        {
            get => repeatType;
            set
            {
                repeatType = value;
                current = RepeatTypeSettings.RepeatTypes.Find(value) ?? RepeatTypeSettings.RepeatTypes.First;
                SetNextCardShowTime();
            }
        }

        [UsedImplicitly]
        public int ShowCount { get; set; }

        [UsedImplicitly]
        public DateTime LastCardShowTime
        {
            get => lastCardShowTime;
            set
            {
                lastCardShowTime = value;
                SetNextCardShowTime();
            }
        }

        [UsedImplicitly, BsonIndex(false)]
        public DateTime NextCardShowTime { get; set; }

        public void DecreaseRepeatType()
        {
            var prev = current.Previous;
            if (prev == null)
                return;

            RepeatType = prev.Value;
            current = prev;
        }

        public void IncreaseRepeatType()
        {
            var next = current.Next;
            if (next == null)
                return;

            RepeatType = next.Value;
            current = next;
        }

        private void SetNextCardShowTime()
        {
            NextCardShowTime = lastCardShowTime.Add(RepeatTypeSettings.RepeatTimes[repeatType]);
        }

        public override string ToString()
        {
            return Key.ToString();
        }
    }
}