using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Classification.Data;

namespace Remembrance.Contracts.Classification
{
    public interface IClassificationClient
    {
        Task<IEnumerable<ClassificationCategory>> GetCategoriesAsync(string text, CancellationToken cancellationToken);
    }
}
