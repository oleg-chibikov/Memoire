using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts
{
    public interface IWord
    {
        PartOfSpeech PartOfSpeech { get; }

        string WordText { get; }
    }
}