using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Translate.Contracts.Data.WordsTranslator;

namespace Remembrance.Translate.Contracts.Interfaces
{
    public interface IWordsTranslator
    {
        [NotNull]
        [ItemNotNull]
        Task<TranslationResult> GetTranslationAsync([NotNull] string from, [NotNull] string to, [NotNull] string text, [NotNull] string ui);
    }
}