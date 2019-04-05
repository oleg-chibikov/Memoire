using System;
using Scar.Common.DAL.Model;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    public sealed class PrepositionsInfo : Entity<TranslationEntryKey>
    {
        public PrepositionsInfo()
        {
        }

        public PrepositionsInfo(TranslationEntryKey translationEntryKey, PrepositionsCollection prepositions)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            Prepositions = prepositions ?? throw new ArgumentNullException(nameof(prepositions));
        }

        public PrepositionsCollection Prepositions { get; set; }

        public override string ToString()
        {
            return $"Prepositions for {Id}";
        }
    }
}