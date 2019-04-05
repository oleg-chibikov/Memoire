using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.Translate
{
    public interface IWordsTranslator
    {
        Task<TranslationResult?> GetTranslationAsync(string from, string to, string text, string ui, CancellationToken cancellationToken);
    }
}