using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Translate.Contracts.Data.LanguageDetector;

namespace Remembrance.Translate.Contracts.Interfaces
{
    public interface ILanguageDetector
    {
        [NotNull, ItemNotNull]
        Task<DetectionResult> DetectLanguageAsync([NotNull] string text);

        [NotNull, ItemNotNull]
        Task<ListResult> ListLanguagesAsync([NotNull] string ui);
    }
}