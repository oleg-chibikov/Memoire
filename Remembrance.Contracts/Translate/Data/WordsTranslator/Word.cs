using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public class Word : BaseWord
    {
        [CanBeNull]
        public string VerbType { get; set; }

        [CanBeNull]
        public string NounAnimacy { get; set; }

        [CanBeNull]
        public string NounGender { get; set; }
    }
}