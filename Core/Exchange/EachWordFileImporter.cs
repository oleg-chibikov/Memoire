using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Processing;
using Mémoire.Core.CardManagement.Data;
using Microsoft.Extensions.Logging;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Core.Exchange
{
    public sealed class EachWordFileImporter : BaseFileImporter<EachWordExchangeEntry>
    {
        static readonly char[] Separator =
        {
            ',',
            ';',
            '/',
            '\\'
        };

        readonly ILanguageManager _languageManager;

        public EachWordFileImporter(
            ITranslationEntryRepository translationEntryRepository,
            ILogger<EachWordFileImporter> logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messenger,
            ILanguageManager languageManager,
            ILearningInfoRepository learningInfoRepository) : base(translationEntryRepository, logger, translationEntryProcessor, messenger, learningInfoRepository)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        protected override IReadOnlyCollection<BaseWord>? GetPriorityTranslations(EachWordExchangeEntry exchangeEntry)
        {
            return exchangeEntry.Translation?.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(text => new BaseWord { Text = text }).ToArray();
        }

        protected override async Task<TranslationEntryKey> GetTranslationEntryKeyAsync(EachWordExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            var sourceLanguage = await _languageManager.GetSourceAutoSubstituteAsync(exchangeEntry.Text, cancellationToken).ConfigureAwait(false);
            var targetLanguage = _languageManager.GetTargetAutoSubstitute(sourceLanguage);
            return new TranslationEntryKey(exchangeEntry.Text, sourceLanguage, targetLanguage);
        }

        protected override bool UpdateLearningInfo(EachWordExchangeEntry exchangeEntry, LearningInfo learningInfo)
        {
            return false;
        }
    }
}
