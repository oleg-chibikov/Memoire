using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class WordImagesInfoRepository : LiteDbRepository<WordImagesInfo, int>, IWordImagesInfoRepository
    {
        public WordImagesInfoRepository([NotNull] ILog logger)
            : base(logger)
        {
            Collection.EnsureIndex(x => x.Id, true);
            Collection.EnsureIndex(x => x.Text);
            Collection.EnsureIndex(x => x.PartOfSpeech);
            Collection.EnsureIndex(x => x.TranslationEntryId);
        }

        [NotNull]
        protected override string DbName => nameof(WordImagesInfo);

        [NotNull]
        protected override string DbPath => Paths.SettingsPath;

        public WordImagesInfo GetImagesInfo(object translationEntryId, IWord word)
        {
            return Collection.FindOne(x => x.TranslationEntryId == translationEntryId && x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech);
        }

        public bool CheckImagesInfoExists(object translationEntryId, IWord word)
        {
            return Collection.Exists(x => x.TranslationEntryId == translationEntryId && x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech);
        }
    }
}