namespace Mémoire.Contracts.Processing.Data
{
    public sealed class TranslationEntryAdditionInfo
    {
        public TranslationEntryAdditionInfo(string? text, string? sourceLanguage = null, string? targetLanguage = null)
        {
            Text = text;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
        }

        public string? SourceLanguage { get; }

        public string? TargetLanguage { get; }

        public string? Text { get; }

        public override string ToString()
        {
            return $"Addition of {Text} [{SourceLanguage}->{TargetLanguage}]";
        }
    }
}
