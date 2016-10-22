using JetBrains.Annotations;
using Remembrance.DAL.Model;

namespace Remembrance.Core
{
    public interface IWordsChecker
    {
        [NotNull]
        TranslationEntry CheckWord([CanBeNull] string text, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null, bool allowExisting = false);
    }
}