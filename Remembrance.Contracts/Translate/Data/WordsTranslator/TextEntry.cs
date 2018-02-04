using Scar.Common;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
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
}