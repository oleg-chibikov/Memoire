using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Core.CardManagement.Data;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal sealed class RemembranceFileImporter : BaseFileImporter<RemembranceExchangeEntry>
    {
        public RemembranceFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
            : base(translationEntryRepository, logger, translationEntryProcessor, messenger, wordsEqualityComparer, wordPriorityRepository)
        {
        }

        protected override async Task<TranslationEntryKey> GetTranslationEntryKeyAsync(RemembranceExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            return await Task.FromResult(exchangeEntry.TranslationEntry.Id).ConfigureAwait(false);
        }

        protected override ManualTranslation[] GetManualTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.ManualTranslations;
        }

        protected override ICollection<ExchangeWord> GetPriorityTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.PriorityTranslations;
        }

        protected override bool SetLearningInfo(RemembranceExchangeEntry exchangeEntry, TranslationEntry translationEntry)
        {
            //TODO: Class LearningInfo. Pass it to TranslationEntryProcessor.AddOrChangeWord
            var changed = translationEntry.IsFavorited != exchangeEntry.TranslationEntry.IsFavorited
                          || translationEntry.RepeatType != exchangeEntry.TranslationEntry.RepeatType
                          || translationEntry.LastCardShowTime != exchangeEntry.TranslationEntry.LastCardShowTime
                          || translationEntry.NextCardShowTime != exchangeEntry.TranslationEntry.NextCardShowTime
                          || translationEntry.ShowCount != exchangeEntry.TranslationEntry.ShowCount;
            translationEntry.IsFavorited = exchangeEntry.TranslationEntry.IsFavorited;
            translationEntry.RepeatType = exchangeEntry.TranslationEntry.RepeatType;
            translationEntry.LastCardShowTime = exchangeEntry.TranslationEntry.LastCardShowTime;
            translationEntry.NextCardShowTime = exchangeEntry.TranslationEntry.NextCardShowTime;
            translationEntry.ShowCount = exchangeEntry.TranslationEntry.ShowCount;
            return changed;
        }
    }
}