using System;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Remembrance.Contracts.DAL.Model
{
    public class BaseWord : TextEntry, IEquatable<BaseWord>
    {
        public BaseWord(BaseWord word)
        {
            _ = word ?? throw new ArgumentNullException(nameof(word));
            PartOfSpeech = word.PartOfSpeech;
            Text = word.Text;
        }

        public BaseWord()
        {
        }

        public PartOfSpeech PartOfSpeech { get; set; }

        public bool Equals(BaseWord other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(Text, other.Text) && PartOfSpeech == other.PartOfSpeech;
        }

        public static bool operator ==(BaseWord? obj1, BaseWord? obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            return obj1?.Equals(obj2!) == true;
        }

        public static bool operator !=(BaseWord? obj1, BaseWord? obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj)
        {
            return obj is BaseWord key && Equals(key);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(Text);
                hashCode = (hashCode * 397) ^ PartOfSpeech.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{Text} ({PartOfSpeech})";
        }
    }
}
