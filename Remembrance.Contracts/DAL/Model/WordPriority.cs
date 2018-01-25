using System;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordPriority : TrackedEntity<WordKey>
    {
        [UsedImplicitly]
        public WordPriority()
        {
        }

        public WordPriority([NotNull] WordKey wordKey)
        {
            Id = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
        }

        public override string ToString()
        {
            return $"Word priority for {Id})";
        }
    }
}