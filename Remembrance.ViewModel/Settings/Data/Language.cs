using JetBrains.Annotations;

namespace Remembrance.ViewModel.Settings.Data
{
    public sealed class Language
    {
        public Language([NotNull] string code, [NotNull] string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        // TODO: Readonly?
        [NotNull]
        [UsedImplicitly]
        public string Code { get; set; }

        [NotNull]
        [UsedImplicitly]
        public string DisplayName { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            return obj is Language cast && Equals(cast);
        }

        private bool Equals([NotNull] Language other)
        {
            return Equals(Code, other.Code) && string.Equals(DisplayName, other.DisplayName);
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
    }
}