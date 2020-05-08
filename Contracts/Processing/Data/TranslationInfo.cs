using System;
using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts.Processing.Data
{
    public sealed class TranslationInfo
    {
        public TranslationInfo(TranslationEntry translationEntry, TranslationDetails translationDetails, LearningInfo learningInfo)
        {
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            TranslationDetails = translationDetails ?? throw new ArgumentNullException(nameof(translationDetails));
            LearningInfo = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
        }

        public LearningInfo LearningInfo { get; set; }

        public TranslationDetails TranslationDetails { get; set; }

        public TranslationEntry TranslationEntry { get; set; }

        public TranslationEntryKey TranslationEntryKey => TranslationEntry.Id;

        public override string ToString()
        {
            return TranslationEntry.ToString();
        }
    }
}
