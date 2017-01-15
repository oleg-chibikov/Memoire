using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.Card.Management.Contracts
{
    public interface IWordsAdder
    {
        [NotNull]
        TranslationInfo AddWordWithChecks([CanBeNull] string text, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null, bool allowExisting = false, int id = 0);

        [NotNull]
        string GetDefaultTargetLanguage([NotNull] string sourceLanguage);
    }
}