using System;
using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationEntryKey : IEquatable<TranslationEntryKey>
    {
        [UsedImplicitly]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public TranslationEntryKey()
        {
        }

        public TranslationEntryKey([NotNull] string text, [NotNull] string sourceLanguage, [NotNull] string targetLanguage)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            SourceLanguage = sourceLanguage ?? throw new ArgumentNullException(nameof(sourceLanguage));
            TargetLanguage = targetLanguage ?? throw new ArgumentNullException(nameof(targetLanguage));
        }

        //TODO: readonly?
        [NotNull]
        [UsedImplicitly]
        public string Text { get; set; }

        [NotNull]
        [UsedImplicitly]
        public string SourceLanguage { get; set; }

        [NotNull]
        [UsedImplicitly]
        public string TargetLanguage { get; set; }

        public bool Equals(TranslationEntryKey other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Text == other.Text && SourceLanguage == other.SourceLanguage && TargetLanguage == other.TargetLanguage;
        }

        public override bool Equals(object obj)
        {
            var key = obj as TranslationEntryKey;
            return key != null && Equals(key);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Text.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceLanguage.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetLanguage.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{Text} [{SourceLanguage}->{TargetLanguage}]";
        }
    }
}