using JetBrains.Annotations;

namespace Remembrance.Settings.ViewModel.Contracts.Data
{
    public sealed class Language
    {
        public Language([NotNull] string code, [NotNull] string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        //TODO: Readonly?
        [NotNull, UsedImplicitly]
        public string Code { get; set; }

        [NotNull, UsedImplicitly]
        public string DisplayName { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            var cast = obj as Language;
            return cast != null && Equals(cast);
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
    }
}