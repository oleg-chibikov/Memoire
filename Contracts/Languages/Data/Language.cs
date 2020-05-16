using System;

namespace Mémoire.Contracts.Languages.Data
{
    public sealed class Language
    {
        public Language(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public string Code { get; }

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
                return (Code.GetHashCode(StringComparison.OrdinalIgnoreCase) * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(DisplayName);
            }
        }

        public override string ToString()
        {
            return Code;
        }

        bool Equals(Language other)
        {
            return Equals(Code, other.Code) && string.Equals(DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
