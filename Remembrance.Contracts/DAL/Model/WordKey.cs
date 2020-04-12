using System;

// ReSharper disable NotNullMemberIsNotInitialized
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordKey : IEquatable<WordKey>
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public WordKey()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
        }

        public WordKey(TranslationEntryKey translationEntryKey, BaseWord word)
        {
            _ = word ?? throw new ArgumentNullException(nameof(word));
            // Creating a new copy to ensure word has the only necessary fields (the WordKeys are stored in DB)
            Word = new BaseWord(word);
            TranslationEntryKey = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
        }

        public TranslationEntryKey TranslationEntryKey { get; set; }

        public BaseWord Word { get; set; }

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

            return Word.Equals(other.Word) && TranslationEntryKey.Equals(other.TranslationEntryKey);
        }

        public static bool operator ==(WordKey? obj1, WordKey? obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            return obj1?.Equals(obj2!) == true;
        }

        public static bool operator !=(WordKey? obj1, WordKey? obj2)
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
                var hashCode = TranslationEntryKey.GetHashCode();
                hashCode = (hashCode * 397) ^ Word.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{TranslationEntryKey} - {Word}";
        }
    }
}
