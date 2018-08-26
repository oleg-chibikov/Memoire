using System;
using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationInfo
    {
        public TranslationInfo([NotNull] TranslationEntry translationEntry, [NotNull] TranslationDetails translationDetails, [NotNull] LearningInfo learningInfo)
        {
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            TranslationDetails = translationDetails ?? throw new ArgumentNullException(nameof(translationDetails));
            LearningInfo = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
        }

        [NotNull]
        public LearningInfo LearningInfo { get; set; }

        [NotNull]
        public TranslationEntry TranslationEntry { get; set; }

        [NotNull]
        public TranslationDetails TranslationDetails { get; set; }

        [NotNull]
        public TranslationEntryKey TranslationEntryKey => TranslationEntry.Id;

        public override string ToString()
        {
            return TranslationEntry.ToString();
        }
    }
}