using System;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class TranslationEntryRepository : TrackedLiteDbRepository<TranslationEntry>, ITranslationEntryRepository
    {
        public TranslationEntryRepository([CanBeNull] string directoryPath = null, [CanBeNull] string fileName = null, bool shrink = true)
            : base(directoryPath ?? Paths.SharedDataPath, fileName, shrink)
        {
            Collection.EnsureIndex(x => x.Key, true);
            Collection.EnsureIndex(x => x.NextCardShowTime);
        }

        public TranslationEntry GetCurrent()
        {
            return Collection.Find(x => x.NextCardShowTime < DateTime.Now) // get entries which are ready to show
                .OrderByDescending(x => x.IsFavorited) // favorited are shown first
                .ThenBy(x => x.ShowCount) // the lower the value, the greater the priority
                .ThenBy(x => Guid.NewGuid()) // similar values are ordered randomly
                .FirstOrDefault();
        }

        public TranslationEntry TryGetByKey(TranslationEntryKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Collection.FindOne(x => x.Key.Text == key.Text && x.Key.SourceLanguage == key.SourceLanguage && x.Key.TargetLanguage == key.TargetLanguage);
        }
    }
}