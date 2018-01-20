using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL
{
    public interface IWordImagesInfoRepository : IRepository<WordImageInfo, int>
    {
        [CanBeNull]
        WordImageInfo GetImageInfo([NotNull] object translationEntryId, [NotNull] IWord word);

        bool CheckImagesInfoExists([NotNull] object translationEntryId, [NotNull] IWord word);

        int DeleteImage([NotNull] object translationEntryId, [NotNull] IWord word);
    }
}