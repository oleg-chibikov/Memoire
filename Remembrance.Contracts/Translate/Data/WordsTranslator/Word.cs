using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class Word : TextEntry, IWord
    {
        [CanBeNull]
        public string VerbType { get; set; }

        [CanBeNull]
        public string NounAnimacy { get; set; }

        [CanBeNull]
        public string NounGender { get; set; }

        public PartOfSpeech PartOfSpeech { get; set; }
    }
}