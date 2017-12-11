using System;
using System.Linq;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class TranslationEntryRepository : LiteDbRepository<TranslationEntry>, ITranslationEntryRepository
    {
        public TranslationEntryRepository([NotNull] ILog logger)
            : base(logger)
        {
            Collection.EnsureIndex(x => x.Id, true);
            Collection.EnsureIndex(x => x.Key, true);
            Collection.EnsureIndex(x => x.NextCardShowTime);
        }

        [NotNull]
        protected override string DbName => "Dictionary";

        [NotNull]
        protected override string DbPath => Paths.SharedDataPath;

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
                throw new ArgumentNullException(nameof(key));

            return Collection.FindOne(x => x.Key.Text == key.Text && x.Key.SourceLanguage == key.SourceLanguage && x.Key.TargetLanguage == key.TargetLanguage);
        }
    }
}