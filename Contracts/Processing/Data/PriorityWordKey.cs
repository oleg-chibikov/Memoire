using System;
using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts.Processing.Data
{
    public sealed class PriorityWordKey
    {
        public PriorityWordKey(bool isPriority, WordKey wordKey)
        {
            IsPriority = isPriority;
            WordKey = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
        }

        public bool IsPriority { get; }

        public WordKey WordKey { get; }

        public override string ToString()
        {
            return $"{WordKey}: {IsPriority}";
        }
    }
}
