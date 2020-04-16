using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Translate.Data.Predictor;

namespace Remembrance.Contracts.Translate
{
    public interface IPredictor
    {
        Task<PredictionResult?> PredictAsync(string text, int limit, CancellationToken cancellationToken);
    }
}
