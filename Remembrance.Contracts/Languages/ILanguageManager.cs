using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Languages.Data;

namespace Remembrance.Contracts.Languages
{
    public interface ILanguageManager
    {
        bool CheckTargetLanguageIsValid([NotNull] string sourceLanguage, string targetLanguage);

        [NotNull]
        IReadOnlyDictionary<string, string> GetAvailableLanguages();

        [NotNull]
        LanguagesCollection GetAvailableSourceLanguages(bool addAuto = true);

        [NotNull]
        LanguagesCollection GetAvailableTargetLanguages([NotNull] string sourceLanguage);

        [NotNull]
        [ItemNotNull]
        Task<string> GetSourceAutoSubstituteAsync([NotNull] string text, CancellationToken cancellationToken);

        [NotNull]
        string GetTargetAutoSubstitute([NotNull] string sourceLanguage);
    }
}