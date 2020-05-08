using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Processing;
using Remembrance.Core.CardManagement.Data;

namespace Remembrance.Core.Exchange
{
    sealed class RemembranceFileImporter : BaseFileImporter<RemembranceExchangeEntry>
    {
        public RemembranceFileImporter(
            ITranslationEntryRepository translationEntryRepository,
            ILogger<RemembranceFileImporter> logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messenger,
            ILearningInfoRepository learningInfoRepository) : base(translationEntryRepository, logger, translationEntryProcessor, messenger, learningInfoRepository)
        {
        }

        protected override IReadOnlyCollection<ManualTranslation>? GetManualTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.ManualTranslations;
        }

        protected override IReadOnlyCollection<BaseWord>? GetPriorityTranslations(RemembranceExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.PriorityWords?.ToArray();
        }

        protected override Task<TranslationEntryKey> GetTranslationEntryKeyAsync(RemembranceExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            return Task.FromResult(exchangeEntry.TranslationEntry.Id);
        }

        protected override bool UpdateLearningInfo(RemembranceExchangeEntry exchangeEntry, LearningInfo learningInfo)
        {
            var changed = (learningInfo.IsFavorited != exchangeEntry.LearningInfo.IsFavorited) ||
                          (learningInfo.RepeatType != exchangeEntry.LearningInfo.RepeatType) ||
                          (learningInfo.LastCardShowTime != exchangeEntry.LearningInfo.LastCardShowTime) ||
                          (learningInfo.NextCardShowTime != exchangeEntry.LearningInfo.NextCardShowTime) ||
                          (learningInfo.ShowCount != exchangeEntry.LearningInfo.ShowCount);
            learningInfo.IsFavorited = exchangeEntry.LearningInfo.IsFavorited;
            learningInfo.RepeatType = exchangeEntry.LearningInfo.RepeatType;
            learningInfo.LastCardShowTime = exchangeEntry.LearningInfo.LastCardShowTime;
            learningInfo.NextCardShowTime = exchangeEntry.LearningInfo.NextCardShowTime;
            learningInfo.ShowCount = exchangeEntry.LearningInfo.ShowCount;
            return changed;
        }
    }
}
