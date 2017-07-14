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
    internal sealed class RemembranceFileImporter : BaseFileImporter<RemembranceExchangeEntry>
    {
        public RemembranceFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] IWordsAdder wordsAdder,
            [NotNull] IMessenger messenger,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository)
            : base(translationEntryRepository, logger, wordsAdder, messenger, translationDetailsRepository)
        {
        }

        protected override TranslationEntryKey GetKey(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.Key;
        }

        protected override ICollection<string> GetPriorityTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.PriorityTranslations;
        }
    }
}