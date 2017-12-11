using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.Predictor;

namespace Remembrance.Contracts.Translate
{
    public interface IPredictor
    {
        [NotNull]
        [ItemNotNull]
        Task<PredictionResult> PredictAsync([NotNull] string text, int limit, CancellationToken cancellationToken);
    }
}