using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
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
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
            : base(translationEntryRepository, logger, wordsProcessor, messenger, wordsEqualityComparer, wordPriorityRepository)
        {
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        }

        protected override async Task<TranslationEntryKey> GetKeyAsync(EachWordExchangeEntry exchangeEntry, CancellationToken cancellationToken)
        {
            var detectionResult = await _languageDetector.DetectLanguageAsync(exchangeEntry.Text, cancellationToken)
                .ConfigureAwait(false);
            var sourceLanguage = detectionResult.Language ?? Constants.EnLanguage;
            var targetLanguage = await WordsProcessor.GetDefaultTargetLanguageAsync(sourceLanguage, cancellationToken)
                .ConfigureAwait(false);
            return new TranslationEntryKey(exchangeEntry.Text, sourceLanguage, targetLanguage);
        }

        protected override ICollection<ExchangeWord> GetPriorityTranslations(EachWordExchangeEntry exchangeEntry)
        {
            return exchangeEntry.Translation?.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(
                    x => new ExchangeWord
                    {
                        Text = x
                    })
                .ToArray();
        }

        protected override bool SetLearningInfo(EachWordExchangeEntry exchangeEntry, TranslationEntry translationEntry)
        {
            return false;
        }
    }
}