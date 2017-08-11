using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts
{
    public interface IWord
    {
        string Text { get; }

        PartOfSpeech PartOfSpeech { get; }
    }
}