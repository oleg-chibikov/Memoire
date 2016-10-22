using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public class Word : TextEntry
    {
        [UsedImplicitly]
        public PartOfSpeech PartOfSpeech { get; set; }

        [CanBeNull, UsedImplicitly]
        public string VerbType { get; set; }

        [CanBeNull, UsedImplicitly]
        public string NounAnimacy { get; set; }

        [CanBeNull, UsedImplicitly]
        public string NounGender { get; set; }
    }
}