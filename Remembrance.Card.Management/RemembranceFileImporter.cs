using System.Collections.Generic;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.Management.Data;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal class RemembranceFileImporter : BaseFileImporter<ExportEntry>
    {
        public RemembranceFileImporter([NotNull] ITranslationEntryRepository translationEntryRepository, [NotNull] ILog logger, [NotNull] IWordsAdder wordsAdder, [NotNull] IMessenger messenger, [NotNull] ITranslationDetailsRepository translationDetailsRepository) : base(translationEntryRepository, logger, wordsAdder, messenger, translationDetailsRepository)
        {
        }

        protected override TranslationEntryKey GetKey(ExportEntry exportEntry)
        {
            return exportEntry.TranslationEntry.Key;
        }

        protected override ICollection<string> GetPriorityTranslations(ExportEntry exportEntry)
        {
            return exportEntry.PriorityTranslations;
        }
    }
}