using System;
using System.Linq;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class TranslationEntryRepository : LiteDbRepository<TranslationEntry>, ITranslationEntryRepository
    {
        public TranslationEntryRepository([NotNull] ILog logger)
            : base(logger)
        {
        }

        protected override string DbName => TranslationDetailsRepository.DictionaryDbName;
        protected override string TableName => nameof(TranslationEntry);

        public TranslationEntry GetCurrent()
        {
            return Db.GetCollection<TranslationEntry>(TableName)
                .Find(x => x.NextCardShowTime < DateTime.Now) //get entries which are ready to show
                .OrderBy(x => x.ShowCount) //the lower the value, the greater the priority
                .FirstOrDefault();
        }

        public TranslationEntry TryGetByKey(TranslationEntryKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Db.GetCollection<TranslationEntry>(TableName).FindOne(x => x.Key.Text == key.Text && x.Key.SourceLanguage == key.SourceLanguage && x.Key.TargetLanguage == key.TargetLanguage);
        }
    }
}