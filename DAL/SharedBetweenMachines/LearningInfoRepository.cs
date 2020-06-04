using System;
using System.Collections.Generic;
using System.Linq;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Scar.Common.DAL.LiteDB;

namespace Mémoire.DAL.SharedBetweenMachines
{
    sealed class LearningInfoRepository : TrackedLiteDbRepository<LearningInfo, TranslationEntryKey>, ILearningInfoRepository
    {
        readonly Random _rand = new Random();
        readonly ISharedSettingsRepository _sharedSettingsRepository;

        public LearningInfoRepository(IPathsProvider pathsProvider, ISharedSettingsRepository sharedSettingsRepository, string? directoryPath = null, bool shrink = false) : base(
            directoryPath ?? pathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(pathsProvider)),
            null,
            shrink)
        {
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
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

            var chooseFavoritedItemsItemsFirst = GetValueByProbability(_sharedSettingsRepository.CardProbabilitySettings.FavoritedItems);
            var chooseItemsWithSmallerShowCountFirst = GetValueByProbability(_sharedSettingsRepository.CardProbabilitySettings.ItemsWithSmallerShowCount);
            var chooseItemsWithLowerRepeatTypeFirst = GetValueByProbability(_sharedSettingsRepository.CardProbabilitySettings.ItemsWithLowerRepeatType);
            var chooseOlderItemsFirst = GetValueByProbability(_sharedSettingsRepository.CardProbabilitySettings.OlderItems);

            var now = DateTime.Now;

            IOrderedEnumerable<LearningInfo> GetExpression()
            {
                return Collection.Find(x => x.NextCardShowTime < now) // get entries which are ready to be shown
                    .OrderByDescending(x => !chooseFavoritedItemsItemsFirst || x.IsFavorited ? 1 : 0) // favorited are shown first
                    .ThenBy(x => chooseItemsWithSmallerShowCountFirst ? x.ShowCount : 0) // the lower the ShowCount, the greater the priority
                    .ThenBy(x => chooseItemsWithLowerRepeatTypeFirst ? x.RepeatType : 0) // the lower the RepeatType, the greater the priority
                    .ThenBy(x => chooseOlderItemsFirst ? x.CreatedDate : DateTime.MinValue) // this gives a chance of showing an older card rather than a newer one first
                    .ThenBy(x => Guid.NewGuid()); // similar values are ordered randomly
            }

            var first = GetExpression().FirstOrDefault();
            if (first == null)
            {
                yield break;
            }

            yield return first;

            if (count == 1)
            {
                yield break;
            }

            var other = GetExpression().Where(x => x.Id != first.Id);
            var firstCategory = first.ClassificationCategories?.Items.FirstOrDefault();

            // Take only the items where one of top 3 categories match the first one
            other = (first.ClassificationCategories != null) && first.ClassificationCategories.Items.Any()
                ? other.Where(x => (x.ClassificationCategories != null) && x.ClassificationCategories.Items.Take(3).Contains(firstCategory))
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

        bool GetValueByProbability(int probability)
        {
            return _rand.Next(100) > 100 - probability;
        }
    }
}
