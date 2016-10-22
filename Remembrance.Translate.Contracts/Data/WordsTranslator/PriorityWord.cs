// ReSharper disable NotNullMemberIsNotInitialized

using System;

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public class PriorityWord : Word
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        public bool IsPriority { get; set; }
    }
}