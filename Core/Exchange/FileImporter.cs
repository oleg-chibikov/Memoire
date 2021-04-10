using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing;
using Mémoire.Core.CardManagement.Data;
using Microsoft.Extensions.Logging;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Core.Exchange
{
    sealed class FileImporter : BaseFileImporter<ExchangeEntry>
    {
        public FileImporter(
            ITranslationEntryRepository translationEntryRepository,
            ILogger<FileImporter> logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messenger,
            ILearningInfoRepository learningInfoRepository) : base(translationEntryRepository, logger, translationEntryProcessor, messenger, learningInfoRepository)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace($"Initializing {GetType().Name}...");
            logger.LogDebug($"Initialized {GetType().Name}");
        }

        protected override IReadOnlyCollection<ManualTranslation>? GetManualTranslations(ExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.ManualTranslations;
        }

        protected override IReadOnlyCollection<BaseWord>? GetPriorityTranslations(ExchangeEntry exchangeEntry)
        {
            return exchangeEntry.TranslationEntry.PriorityWords?.ToArray();
        }

        protected override Task<TranslationEntryKey> GetTranslationEntryKeyAsync(ExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            return Task.FromResult(exchangeEntry.TranslationEntry.Id);
        }

        protected override bool UpdateLearningInfo(ExchangeEntry exchangeEntry, LearningInfo learningInfo)
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
