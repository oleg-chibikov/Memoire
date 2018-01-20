using System;
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
    internal sealed class WordImagesInfoRepository : LiteDbRepository<WordImageInfo, int>, IWordImagesInfoRepository
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
        protected override string DbName => nameof(WordImageInfo);

        [NotNull]
        protected override string DbPath => Paths.SettingsPath;

        public WordImageInfo GetImageInfo(object translationEntryId, IWord word)
        {
            if (translationEntryId == null)
            {
                throw new ArgumentNullException(nameof(translationEntryId));
            }

            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            return Collection.FindOne(x => x.TranslationEntryId.Equals(translationEntryId) && x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech);
        }

        public bool CheckImagesInfoExists(object translationEntryId, IWord word)
        {
            if (translationEntryId == null)
            {
                throw new ArgumentNullException(nameof(translationEntryId));
            }

            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            return Collection.Exists(x => x.TranslationEntryId.Equals(translationEntryId) && x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech);
        }

        public int DeleteImage(object translationEntryId, IWord word)
        {
            if (translationEntryId == null)
            {
                throw new ArgumentNullException(nameof(translationEntryId));
            }

            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            return Collection.Delete(x => x.TranslationEntryId.Equals(translationEntryId) && x.Text == word.Text && x.PartOfSpeech == word.PartOfSpeech);
        }
    }
}