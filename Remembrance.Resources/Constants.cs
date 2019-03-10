using JetBrains.Annotations;

namespace Remembrance.Resources
{
    public static class Constants
    {
        [NotNull]
        public const string AutoDetectLanguage = "auto";

        [NotNull]
        public const string EnLanguage = "en-US";

        [NotNull]
        public const string EnLanguageTwoLetters = "en";

        [NotNull]
        public const string RuLanguage = "ru-RU";

        [NotNull]
        public const string RuLanguageTwoLetters = "ru";

        [NotNull]
        public const string ReversoContextUrlTemplate = "https://context.reverso.net/translation/{0}-{1}/{2}";
    }
}