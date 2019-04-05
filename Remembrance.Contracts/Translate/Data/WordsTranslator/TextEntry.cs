namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class TextEntry
    {
        public virtual string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}