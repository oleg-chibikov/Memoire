using System.Collections.Generic;
using System.Threading;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Core.CardManagement.Data;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal sealed class RemembranceFileImporter : BaseFileImporter<RemembranceExchangeEntry>
    {
        public RemembranceFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IViewModelAdapter viewModelAdapter)
            : base(translationEntryRepository, logger, wordsProcessor, messenger, wordsEqualityComparer, wordPriorityRepository, viewModelAdapter)
        {
        }

        protected override TranslationEntryKey GetKey(RemembranceExchangeEntry exchangeEntry, CancellationToken token)
        {
            return exchangeEntry.TranslationEntry.Key;
        }

        protected override ICollection<ExchangeWord> GetPriorityTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.PriorityTranslations;
        }
    }
}