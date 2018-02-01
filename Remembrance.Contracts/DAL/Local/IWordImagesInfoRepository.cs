using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Local
{
    public interface IWordImagesInfoRepository : IRepository<WordImageInfo, WordKey>
    {
        void ClearForTranslationEntry([NotNull] TranslationEntryKey translationEntryKey);
    }
}