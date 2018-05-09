using JetBrains.Annotations;

namespace Remembrance.Contracts.Languages.Data
{
    public sealed class Language
    {
        public Language([NotNull] string code, [NotNull] string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        [NotNull]
        public string Code { get; }

        [NotNull]
        public string DisplayName { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is Language cast && Equals(cast);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Code.GetHashCode() * 397) ^ DisplayName.GetHashCode();
            }
        }

        public override string ToString()
        {
            return Code;
        }

        private bool Equals([NotNull] Language other)
        {
            return Equals(Code, other.Code) && string.Equals(DisplayName, other.DisplayName);
        }
    }
}