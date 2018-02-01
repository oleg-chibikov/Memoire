using JetBrains.Annotations;
using Scar.Common;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class Word : WordTextEntry, IWord
    {
        [CanBeNull]
        public string VerbType { get; set; }

        [CanBeNull]
        public string NounAnimacy { get; set; }

        [CanBeNull]
        public string NounGender { get; set; }

        public PartOfSpeech PartOfSpeech { get; set; }
    }

    public class TextEntry
    {
        private string _text;

        public virtual string Text
        {
            get => _text;
            set => _text = value.Capitalize();
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class WordTextEntry
    {
        private string _wordText;

        public virtual string WordText
        {
            get => _wordText;
            set => _wordText = value.Capitalize();
        }

        public override string ToString()
        {
            return WordText;
        }
    }
}