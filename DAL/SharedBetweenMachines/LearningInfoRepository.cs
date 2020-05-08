using System;
using System.Collections.Generic;
using System.Linq;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.SharedBetweenMachines
{
    sealed class LearningInfoRepository : TrackedLiteDbRepository<LearningInfo, TranslationEntryKey>, ILearningInfoRepository
    {
        readonly Random _rand = new Random();

        public LearningInfoRepository(IRemembrancePathsProvider remembrancePathsProvider, string? directoryPath = null, bool shrink = true) : base(
            directoryPath ?? remembrancePathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(remembrancePathsProvider)),
            null,
            shrink)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
            Collection.EnsureIndex(x => x.NextCardShowTime);
        }

        public IEnumerable<LearningInfo> GetMostSuitable(int count)
        {
            if (count == 0)
            {
                yield break;
            }

            var chooseFavoritedItemsItemsFirst = _rand.Next(100) > 20;
            var chooseItemsWithSmallerShowCountFirst = _rand.Next(100) > 30;
            var chooseItemsWithLowerRepeatTypeFirst = _rand.Next(100) > 30;
            var chooseOlderItemsFirst = _rand.Next(100) > 30;

            var now = DateTime.Now;

            IOrderedEnumerable<LearningInfo> GetExpression()
            {
                return Collection.Find(x => x.NextCardShowTime < now) // get entries which are ready to be shown
                    .OrderByDescending(x => !chooseFavoritedItemsItemsFirst || x.IsFavorited) // favorited are shown first with 20% probability
                    .ThenBy(x => chooseItemsWithSmallerShowCountFirst ? x.ShowCount : 0) // the lower the ShowCount, the greater the priority. This rule will be applied in 30% cases
                    .ThenBy(x => chooseItemsWithLowerRepeatTypeFirst ? x.RepeatType : 0) // the lower the RepeatType, the greater the priority. This rule will be applied in 30% cases
                    .ThenBy(x => chooseOlderItemsFirst ? x.CreatedDate : DateTime.MinValue) // this gives a 30% chance of showing an old card
                    .ThenBy(x => Guid.NewGuid()); // similar values are ordered randomly
            }

            var first = GetExpression().FirstOrDefault();
            yield return first;

            if (count == 1)
            {
                yield break;
            }

            var other = GetExpression().Skip(1);
            var firstCategory = first.ClassificationCategories?.Items.FirstOrDefault();

            // Take only the items where one of top 3 categories match the first one
            other = first.ClassificationCategories != null && first.ClassificationCategories.Items.Count > 0
                ? other.Where(x => x.ClassificationCategories != null && x.ClassificationCategories.Items.Take(3).Contains(firstCategory))
                : other;
            other = other.Take(count - 1);

            foreach (var item in other)
            {
                yield return item;
            }
        }

        public LearningInfo GetOrInsert(TranslationEntryKey translationEntryKey)
        {
            var result = TryGetById(translationEntryKey);
            if (result == null)
            {
                result = new LearningInfo { Id = translationEntryKey };
                Insert(result);
            }

            return result;
        }
    }
}
