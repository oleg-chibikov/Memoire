using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Classification.Data;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.Contracts.Classification
{
    public interface ILearningInfoCategoriesUpdater
    {
        Task<IReadOnlyCollection<ClassificationCategory>> UpdateLearningInfoClassificationCategoriesAsync(TranslationInfo translationInfo, CancellationToken cancellationToken);
    }
}
