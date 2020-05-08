using System;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationDetails : Entity<TranslationEntryKey>
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public TranslationDetails()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
        }

        public TranslationDetails(TranslationResult translationResult, TranslationEntryKey translationEntryKey)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
        }

        public TranslationResult TranslationResult { get; set; }

        public override string ToString()
        {
            return $"Translation details for {Id}";
        }
    }
}
