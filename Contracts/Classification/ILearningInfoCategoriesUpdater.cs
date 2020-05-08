using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.Processing.Data;

namespace Mémoire.Contracts.Classification
{
    public interface ILearningInfoCategoriesUpdater
    {
        Task UpdateLearningInfoClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken);
    }
}
