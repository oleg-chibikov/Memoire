using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class Word : BaseWord
    {
        public string? NounAnimacy { get; set; }

        public string? NounGender { get; set; }

        public string? VerbType { get; set; }
    }
}
