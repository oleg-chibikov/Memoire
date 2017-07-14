using JetBrains.Annotations;
using Scar.Common;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public class TextEntry
    {
        private string _text;

        [NotNull]
        [UsedImplicitly]
        public string Text
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