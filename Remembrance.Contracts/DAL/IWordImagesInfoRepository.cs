using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL
{
    public interface IWordImagesInfoRepository : IRepository<WordImagesInfo, int>
    {
        [CanBeNull]
        WordImagesInfo GetImagesInfo([NotNull] object translationEntryId, [NotNull] IWord word);

        bool CheckImagesInfoExists([NotNull] object translationEntryId, [NotNull] IWord word);
    }
}