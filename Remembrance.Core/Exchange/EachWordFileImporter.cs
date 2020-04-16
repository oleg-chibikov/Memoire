using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Processing;
using Remembrance.Core.CardManagement.Data;

namespace Remembrance.Core.Exchange
{
    sealed class EachWordFileImporter : BaseFileImporter<EachWordExchangeEntry>
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
            ILog logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messenger,
            ILanguageManager languageManager,
            ILearningInfoRepository learningInfoRepository)
            : base(translationEntryRepository, logger, translationEntryProcessor, messenger, learningInfoRepository)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
        }

        protected override IReadOnlyCollection<BaseWord>? GetPriorityTranslations(EachWordExchangeEntry exchangeEntry)
        {
            return exchangeEntry.Translation?.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(
                    text => new BaseWord
                    {
                        Text = text
                    })
                .ToArray();
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