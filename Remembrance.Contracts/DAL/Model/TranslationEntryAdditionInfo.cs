using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    public class TranslationEntryAdditionInfo
    {
        public TranslationEntryAdditionInfo([CanBeNull] string text, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null)
        {
            Text = text;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
        }

        [CanBeNull]
        public string Text { get; set; }

        [CanBeNull]
        public string SourceLanguage { get; set; }

        [CanBeNull]
        public string TargetLanguage { get; set; }

        public override string ToString()
        {
            return $"{Text} [{SourceLanguage}->{TargetLanguage}]";
        }
    }
}