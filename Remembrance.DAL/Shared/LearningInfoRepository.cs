using System;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class LearningInfoRepository : TrackedLiteDbRepository<LearningInfo, TranslationEntryKey>, ILearningInfoRepository
    {
        public LearningInfoRepository([CanBeNull] string directoryPath = null, bool shrink = true)
            : base(directoryPath ?? RemembrancePaths.LocalSharedDataPath, null, shrink)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
            Collection.EnsureIndex(x => x.NextCardShowTime);
        }

        public LearningInfo GetMostSuitable()
        {
            return Collection.Find(x => x.NextCardShowTime < DateTime.Now) // get entries which are ready to show
                .OrderByDescending(x => x.IsFavorited) // favorited are shown first
                .ThenBy(x => x.ShowCount) // the lower the value, the greater the priority
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