// ReSharper disable NotNullMemberIsNotInitialized

using System;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class PriorityWord : Word
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        public bool IsPriority { get; set; }
    }
}