using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordKey : TranslationEntryKey, IEquatable<WordKey>, IWord
    {
        [UsedImplicitly]
        public WordKey()
        {
        }

        public WordKey([NotNull] TranslationEntryKey translationEntryKey, [NotNull] IWord word)
            : base(translationEntryKey.Text, translationEntryKey.SourceLanguage, translationEntryKey.TargetLanguage)
        {
            WordText = word?.WordText ?? throw new ArgumentNullException(nameof(word));
            PartOfSpeech = word.PartOfSpeech;
        }

        public bool Equals(WordKey other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return WordText == other.WordText && PartOfSpeech == other.PartOfSpeech && base.Equals(other);
        }

        public string WordText { get; set; }

        public PartOfSpeech PartOfSpeech { get; set; }

        public static bool operator ==([CanBeNull] WordKey obj1, [CanBeNull] WordKey obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            return obj1?.Equals(obj2) == true;
        }

        // this is second one '!='
        public static bool operator !=([CanBeNull] WordKey obj1, [CanBeNull] WordKey obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj)
        {
            return obj is WordKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ WordText.GetHashCode();
                hashCode = (hashCode * 397) ^ PartOfSpeech.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()} - {WordText} ({PartOfSpeech})";
        }
    }
}