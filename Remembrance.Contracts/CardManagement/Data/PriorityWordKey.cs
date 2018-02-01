using System;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Contracts.CardManagement.Data
{
    public sealed class PriorityWordKey
    {
        public PriorityWordKey(bool isPriority, [NotNull] WordKey wordKey)
        {
            IsPriority = isPriority;
            WordKey = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
        }

        public bool IsPriority { get; }

        [NotNull]
        public WordKey WordKey { get; }

        public override string ToString()
        {
            return $"{WordKey}: {IsPriority}";
        }
    }
}