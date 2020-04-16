using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.Translate
{
    public interface IWordsTranslator
    {
        Task<TranslationResult?> GetTranslationAsync(string sourceLanguage, string targetLanguage, string text, string ui, CancellationToken cancellationToken);
    }
}
