using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        protected override async Task<TranslationEntryKey> GetKeyAsync(RemembranceExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            return await Task.FromResult(exchangeEntry.TranslationEntry.Key).ConfigureAwait(false);
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
            //TODO: Class LearningInfo. Pass it to WordProcessor.AddOrChangeWord
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