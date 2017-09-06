using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Contracts.CardManagement
{
    public interface IWordsProcessor
    {
        [NotNull]
        string GetDefaultTargetLanguage([NotNull] string sourceLanguage);

        [NotNull]
        TranslationInfo AddOrChangeWord([NotNull] string text, [CanBeNull] string sourceLanguage = null, [CanBeNull] string targetLanguage = null, IWindow ownerWindow = null, bool needPostProcess = true, object id = null);

        [NotNull]
        TranslationDetails ReloadTranslationDetailsIfNeeded([NotNull] object id, [NotNull] string text, [NotNull] string sourceLanguage, [NotNull] string targetLanguage);
    }
}