using System;
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
using Remembrance.Contracts.Translate;
using Remembrance.Core.CardManagement.Data;
using Remembrance.Resources;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal sealed class EachWordFileImporter : BaseFileImporter<EachWordExchangeEntry>
    {
        private static readonly char[] Separator =
        {
            ',',
            ';',
            '/',
            '\\'
        };

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        public EachWordFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] ILearningInfoRepository learningInfoRepository)
            : base(translationEntryRepository, logger, translationEntryProcessor, messenger, learningInfoRepository)
        {
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        }

        protected override async Task<TranslationEntryKey> GetTranslationEntryKeyAsync(EachWordExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            var detectionResult = await _languageDetector.DetectLanguageAsync(exchangeEntry.Text, cancellationToken).ConfigureAwait(false);
            var sourceLanguage = detectionResult.Language ?? Constants.EnLanguage;
            var targetLanguage = await TranslationEntryProcessor.GetDefaultTargetLanguageAsync(sourceLanguage, cancellationToken).ConfigureAwait(false);
            return new TranslationEntryKey(exchangeEntry.Text, sourceLanguage, targetLanguage);
        }

        protected override ICollection<BaseWord> GetPriorityTranslations(EachWordExchangeEntry exchangeEntry)
        {
            return exchangeEntry.Translation?.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(
                    text => new BaseWord
                    {
                        Text = text
                    })
                .ToArray();
        }

        protected override bool UpdateLearningInfo(EachWordExchangeEntry exchangeEntry, LearningInfo learningInfo)
        {
            return false;
        }
    }
}