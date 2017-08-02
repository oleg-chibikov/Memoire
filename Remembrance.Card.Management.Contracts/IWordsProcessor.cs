using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.Card.Management.Contracts
{
    public interface IWordsProcessor
    {
        [NotNull]
        TranslationInfo AddWord([CanBeNull] string text, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null, [CanBeNull] object id = null);

        bool ChangeWord([NotNull] object id, [NotNull] string text, [NotNull] string sourceLanguage, [NotNull] string targetLanguage, bool showCard = true);

        [NotNull]
        string GetDefaultTargetLanguage([NotNull] string sourceLanguage);

        bool ProcessNewWord([NotNull] string word, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null, bool showCard = true);

        [NotNull]
        TranslationInfo ReloadTranslationDetailsIfNeeded([NotNull] TranslationEntry translationEntry);
    }
}