using JetBrains.Annotations;
using Scar.Common;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public class TextEntry
    {
        private string text;

        [NotNull, UsedImplicitly]
        public string Text
        {
            get { return text; }
            set { text = value.Capitalize(); }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}