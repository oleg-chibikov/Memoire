using JetBrains.Annotations;
using Scar.Common;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class TextEntry : IWithText
    {
        private string _text;

        [NotNull]
        [UsedImplicitly]
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