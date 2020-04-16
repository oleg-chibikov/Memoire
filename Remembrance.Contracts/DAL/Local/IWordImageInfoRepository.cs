using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Local
{
    public interface IWordImageInfoRepository : IRepository<WordImageInfo, WordKey>
    {
        void ClearForTranslationEntry(TranslationEntryKey translationEntryKey);
    }
}
