using System;
using Scar.Common.DAL.Model;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsInfo : Entity<TranslationEntryKey>
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public PrepositionsInfo()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
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
