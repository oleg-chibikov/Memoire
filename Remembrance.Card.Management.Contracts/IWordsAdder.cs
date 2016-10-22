using JetBrains.Annotations;

namespace Remembrance.Card.Management.Contracts
{
    public interface IWordsAdder
    {
        void AddWord([NotNull] string word, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null);
    }
}