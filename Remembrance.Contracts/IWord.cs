using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts
{
    public interface IWord : IWithText
    {
        PartOfSpeech PartOfSpeech { get; }
    }
}