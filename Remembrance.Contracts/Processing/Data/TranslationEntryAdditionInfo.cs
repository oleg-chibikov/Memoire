using JetBrains.Annotations;

namespace Remembrance.Contracts.Processing.Data
{
    public sealed class TranslationEntryAdditionInfo
    {
        public TranslationEntryAdditionInfo([CanBeNull] string? text, [CanBeNull] string? sourceLanguage = null, [CanBeNull] string? targetLanguage = null)
        {
            Text = text;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
        }

        [CanBeNull]
        public string? SourceLanguage { get; }

        [CanBeNull]
        public string? TargetLanguage { get; }

        [CanBeNull]
        public string? Text { get; }

        public override string ToString()
        {
            return $"Addition of {Text} [{SourceLanguage}->{TargetLanguage}]";
        }
    }
}