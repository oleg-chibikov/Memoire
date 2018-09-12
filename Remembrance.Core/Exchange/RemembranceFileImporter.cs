using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
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
            [NotNull] ILearningInfoRepository learningInfoRepository)
            : base(translationEntryRepository, logger, translationEntryProcessor, messenger, learningInfoRepository)
        {
        }

        protected override IReadOnlyCollection<ManualTranslation> GetManualTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.ManualTranslations;
        }

        protected override IReadOnlyCollection<BaseWord> GetPriorityTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.PriorityWords?.ToArray();
        }

        protected override async Task<TranslationEntryKey> GetTranslationEntryKeyAsync(RemembranceExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            return await Task.FromResult(exchangeEntry.TranslationEntry.Id).ConfigureAwait(false);
        }

        protected override bool UpdateLearningInfo(RemembranceExchangeEntry exchangeEntry, LearningInfo learningInfo)
        {
            var changed = learningInfo.IsFavorited != exchangeEntry.LearningInfo.IsFavorited
                          || learningInfo.RepeatType != exchangeEntry.LearningInfo.RepeatType
                          || learningInfo.LastCardShowTime != exchangeEntry.LearningInfo.LastCardShowTime
                          || learningInfo.NextCardShowTime != exchangeEntry.LearningInfo.NextCardShowTime
                          || learningInfo.ShowCount != exchangeEntry.LearningInfo.ShowCount;
            learningInfo.IsFavorited = exchangeEntry.LearningInfo.IsFavorited;
            learningInfo.RepeatType = exchangeEntry.LearningInfo.RepeatType;
            learningInfo.LastCardShowTime = exchangeEntry.LearningInfo.LastCardShowTime;
            learningInfo.NextCardShowTime = exchangeEntry.LearningInfo.NextCardShowTime;
            learningInfo.ShowCount = exchangeEntry.LearningInfo.ShowCount;
            return changed;
        }
    }
}