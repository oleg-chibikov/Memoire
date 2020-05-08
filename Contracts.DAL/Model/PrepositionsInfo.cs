using System;
using Scar.Common.DAL.Contracts.Model;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PrepositionsInfo : Entity<TranslationEntryKey>
    {
        public PrepositionsInfo()
        {
        }

        public PrepositionsInfo(TranslationEntryKey translationEntryKey, Prepositions prepositions)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            Prepositions = prepositions ?? throw new ArgumentNullException(nameof(prepositions));
        }

        public Prepositions Prepositions { get; set; }

        public override string ToString()
        {
            return $"Prepositions for {Id}";
        }
    }
}
