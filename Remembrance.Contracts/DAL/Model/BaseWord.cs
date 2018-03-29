using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable NonReadonlyMemberInGetHashCode
namespace Remembrance.Contracts.DAL.Model
{
    public class BaseWord : TextEntry, IEquatable<BaseWord>
    {
        public BaseWord([NotNull] BaseWord word)
        {
            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            PartOfSpeech = word.PartOfSpeech;
            Text = word.Text;
        }

        [UsedImplicitly]
        public BaseWord()
        {
        }

        public virtual PartOfSpeech PartOfSpeech { get; set; }

        public static bool operator ==([CanBeNull] BaseWord obj1, [CanBeNull] BaseWord obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            return obj1?.Equals(obj2) == true;
        }

        public static bool operator !=([CanBeNull] BaseWord obj1, [CanBeNull] BaseWord obj2)
        {
            return !(obj1 == obj2);
        }

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

            return Text == other.Text && PartOfSpeech == other.PartOfSpeech;
        }

        public override bool Equals(object obj)
        {
            return obj is BaseWord key && Equals(key);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Text.GetHashCode();
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