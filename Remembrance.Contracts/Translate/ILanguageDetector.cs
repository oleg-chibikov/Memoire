using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Contracts.Translate
{
    public interface ILanguageDetector
    {
        [ItemNotNull]
        [NotNull]
        Task<DetectionResult> DetectLanguageAsync([NotNull] string text, CancellationToken cancellationToken);

        [ItemCanBeNull]
        [NotNull]
        Task<LanguageListResult> ListLanguagesAsync([NotNull] string ui, CancellationToken cancellationToken);
    }
}