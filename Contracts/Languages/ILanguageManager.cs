using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Languages.Data;

namespace Remembrance.Contracts.Languages
{
    public interface ILanguageManager
    {
        bool CheckTargetLanguageIsValid(string sourceLanguage, string targetLanguage);

        IReadOnlyDictionary<string, string> GetAvailableLanguages();

        LanguagesCollection GetAvailableSourceLanguages(bool addAuto = true);

        LanguagesCollection GetAvailableTargetLanguages(string sourceLanguage);

        Task<string> GetSourceAutoSubstituteAsync(string text, CancellationToken cancellationToken);

        string GetTargetAutoSubstitute(string sourceLanguage);
    }
}
