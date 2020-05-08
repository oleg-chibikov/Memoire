using Mémoire.Contracts.DAL.Model;
using Scar.Common.DAL.Contracts;

namespace Mémoire.Contracts.DAL.Local
{
    public interface IWordImageInfoRepository : IRepository<WordImageInfo, WordKey>
    {
        void ClearForTranslationEntry(TranslationEntryKey translationEntryKey);
    }
}
