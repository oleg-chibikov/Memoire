namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class TextEntry
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public virtual string Text { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public override string ToString()
        {
            return Text;
        }
    }
}
