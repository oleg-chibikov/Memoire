using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Contracts.Translate
{
    public interface ILanguageDetector
    {
        [NotNull]
        [ItemNotNull]
        Task<DetectionResult> DetectLanguageAsync([NotNull] string text, CancellationToken cancellationToken);

        [NotNull]
        [ItemNotNull]
        Task<ListResult> ListLanguagesAsync([NotNull] string ui, CancellationToken cancellationToken);
    }
}