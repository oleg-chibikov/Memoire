using System;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsInfo : Entity<TranslationEntryKey>
    {
        [UsedImplicitly]
        public PrepositionsInfo()
        {
        }

        public PrepositionsInfo([NotNull] TranslationEntryKey translationEntryKey, [NotNull] PrepositionsCollection prepositions)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
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