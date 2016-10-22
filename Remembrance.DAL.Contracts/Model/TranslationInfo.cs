using System;
using JetBrains.Annotations;

namespace Remembrance.DAL.Contracts.Model
{
    public class TranslationInfo
    {
        public TranslationInfo([NotNull] TranslationEntry translationEntry, [NotNull] TranslationDetails translationDetails)
        {
            if (translationEntry == null)
                throw new ArgumentNullException(nameof(translationEntry));
            if (translationDetails == null)
                throw new ArgumentNullException(nameof(translationDetails));
            TranslationEntry = translationEntry;
            TranslationDetails = translationDetails;
        }

        [NotNull]
        public TranslationEntry TranslationEntry { get; set; }

        [NotNull]
        public TranslationDetails TranslationDetails { get; set; }

        [NotNull]
        public TranslationEntryKey Key => TranslationEntry.Key;

        public override string ToString()
        {
            return TranslationEntry.ToString();
        }
    }
}