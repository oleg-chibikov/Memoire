using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Contracts.Translate
{
    public interface ILanguageDetector
    {
        Task<DetectionResult> DetectLanguageAsync(string text, CancellationToken cancellationToken);

        Task<LanguageListResult?> ListLanguagesAsync(string ui, CancellationToken cancellationToken);
    }
}
