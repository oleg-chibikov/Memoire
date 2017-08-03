using System;
using JetBrains.Annotations;

namespace Remembrance.ViewModel
{
    public class PriorityWordLink
    {
        public PriorityWordLink(Guid correlationId, [NotNull] object translationEntryId, bool isPriority)
        {
            CorrelationId = correlationId;
            IsPriority = isPriority;
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));

        }

        public Guid CorrelationId { get;}
        public object TranslationEntryId { get; }
        public bool IsPriority { get; }
    }
}
