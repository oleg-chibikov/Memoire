using System;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsInfo : Entity<int>
    {
        [UsedImplicitly]
        public PrepositionsInfo()
        {
        }

        public PrepositionsInfo([NotNull] object translationEntryId, [NotNull] PrepositionsCollection prepositions)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            Prepositions = prepositions ?? throw new ArgumentNullException(nameof(prepositions));
        }

        [NotNull]
        public object TranslationEntryId { get; set; }

        [NotNull]
        public PrepositionsCollection Prepositions { get; set; }

        public override string ToString()
        {
            return $"Prepositions for {TranslationEntryId}";
        }
    }
}