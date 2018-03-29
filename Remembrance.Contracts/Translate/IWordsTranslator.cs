using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts.Translate
{
    public interface IWordsTranslator
    {
        [ItemCanBeNull]
        [NotNull]
        Task<TranslationResult> GetTranslationAsync([NotNull] string from, [NotNull] string to, [NotNull] string text, [NotNull] string ui, CancellationToken cancellationToken);
    }
}