using System;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsInfo : Entity
    {
        [UsedImplicitly]
        public PrepositionsInfo()
        {
        }

        public PrepositionsInfo([NotNull] object translationEntryId, [NotNull] PrepositionsCollection prepositions)
        {
            Id = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            Prepositions = prepositions ?? throw new ArgumentNullException(nameof(prepositions));
        }

        [NotNull]
        public PrepositionsCollection Prepositions { get; set; }

        public override string ToString()
        {
            return $"Prepositions for {Id}";
        }
    }
}