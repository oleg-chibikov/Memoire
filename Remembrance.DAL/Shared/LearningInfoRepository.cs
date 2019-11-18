using System;
using System.Linq;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    internal sealed class LearningInfoRepository : TrackedLiteDbRepository<LearningInfo, TranslationEntryKey>, ILearningInfoRepository
    {
        private readonly Random _rand = new Random();

        public LearningInfoRepository(IRemembrancePathsProvider remembrancePathsProvider, string? directoryPath = null, bool shrink = true)
            : base(directoryPath ?? remembrancePathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(remembrancePathsProvider)), null, shrink)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
            Collection.EnsureIndex(x => x.NextCardShowTime);
        }

        public LearningInfo? GetMostSuitable()
        {
            var chooseFavoritedItemsItemsFirst = _rand.Next(100) > 20;
            var chooseItemsWithSmallerShowCountFirst = _rand.Next(100) > 30;
            var chooseItemsWithLowerRepeatTypeFirst = _rand.Next(100) > 30;
            var chooseOlderItemsFirst = _rand.Next(100) > 30;
            return Collection.Find(x => x.NextCardShowTime < DateTime.Now) // get entries which are ready to be shown
                .OrderByDescending(x => !chooseFavoritedItemsItemsFirst || x.IsFavorited) // favorited are shown first with 20% probability
                .ThenBy(x => chooseItemsWithSmallerShowCountFirst ? x.ShowCount : 0) // the lower the ShowCount, the greater the priority. This rule will be applied in 30% cases
                .ThenBy(x => chooseItemsWithLowerRepeatTypeFirst ? x.RepeatType : 0) // the lower the RepeatType, the greater the priority. This rule will be applied in 30% cases
                .ThenBy(x => chooseOlderItemsFirst ? x.CreatedDate : DateTime.MinValue) // this gives a 30% chance of showing an old card
                .ThenBy(x => Guid.NewGuid()) // similar values are ordered randomly
                .FirstOrDefault();
        }

        public LearningInfo GetOrInsert(TranslationEntryKey translationEntryKey)
        {
            var result = TryGetById(translationEntryKey);
            if (result == null)
            {
                result = new LearningInfo
                {
                    Id = translationEntryKey
                };
                Insert(result);
            }

            return result;
        }
    }
}