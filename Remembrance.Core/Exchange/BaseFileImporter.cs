using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Core.CardManagement.Data;
using Remembrance.Resources;
using Remembrance.ViewModel.Translation;
using Scar.Common;
using Scar.Common.Events;
using Scar.Common.Exceptions;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal abstract class BaseFileImporter<T> : IFileImporter, IDisposable
        where T : IExchangeEntry
    {
        private const int MaxBlockSize = 25;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        [NotNull]
        protected readonly IWordsProcessor WordsProcessor;

        protected BaseFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] IMessenger messenger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IViewModelAdapter viewModelAdapter)
        {
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            WordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public void Dispose()
        {
            _messenger.Unregister(this);
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public async Task<ExchangeResult> ImportAsync(string fileName, CancellationToken token)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            string[] errors = null;
            var e = new List<string>();
            var count = 0;
            T[] deserialized;

            return await Task.Run(
                () =>
                {
                    try
                    {
                        var file = File.ReadAllText(fileName);
                        deserialized = JsonConvert.DeserializeObject<T[]>(file);
                    }
                    catch (IOException ex)
                    {
                        _logger.Warn("Cannot load file from disk", ex);
                        return new ExchangeResult(false, null, count);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("Cannot deserialize object", ex);
                        return new ExchangeResult(false, null, count);
                    }

                    _logger.Trace("Getting all translations...");
                    var existing = _translationEntryRepository.GetAll();
                    var existingKeys = new HashSet<TranslationEntryKey>(existing.Select(x => x.Key));

                    deserialized.RunByBlocks(
                        MaxBlockSize,
                        (block, index, blocksCount) =>
                        {
                            token.ThrowIfCancellationRequested();
                            _logger.Trace($"Processing block {index} out of {blocksCount} ({block.Length} files)...");
                            var blockResult = new List<TranslationInfo>(block.Length);
                            foreach (var exchangeEntry in block)
                            {
                                token.ThrowIfCancellationRequested();
                                _logger.Info($"Importing from {exchangeEntry.Text}...");
                                var priorityTranslations = GetPriorityTranslations(exchangeEntry);
                                try
                                {
                                    var key = GetKey(exchangeEntry, token);
                                    if (priorityTranslations == null && existingKeys.Contains(key))
                                        continue;

                                    var translationInfo = WordsProcessor.AddOrChangeWord(key.Text, key.SourceLanguage, key.TargetLanguage, null, false);
                                    if (priorityTranslations != null)
                                        if (ImportPriority(priorityTranslations, translationInfo.TranslationDetails) == 0)
                                            continue; //No priority imported

                                    blockResult.Add(translationInfo);
                                }
                                catch (LocalizableException ex)
                                {
                                    _logger.Warn($"Cannot translate {exchangeEntry.Text}. The word is skipped", ex);
                                    e.Add(exchangeEntry.Text);
                                    continue;
                                }

                                count++;
                            }

                            OnProgress(index + 1, blocksCount);
                            if (blockResult.Any())
                                _messenger.Send(blockResult.ToArray(), MessengerTokens.TranslationInfoBatchToken);
                            return true;
                        });

                    if (e.Any())
                        errors = e.ToArray();
                    return new ExchangeResult(true, errors, count);
                },
                token);
        }

        [NotNull]
        protected abstract TranslationEntryKey GetKey([NotNull] T exchangeEntry, CancellationToken token);

        [CanBeNull]
        protected abstract ICollection<ExchangeWord> GetPriorityTranslations([NotNull] T exchangeEntry);

        private int ImportPriority([NotNull] ICollection<ExchangeWord> priorityTranslations, [NotNull] TranslationDetails translationDetails)
        {
            var result = 0;
            foreach (var translationVariant in translationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (MarkPriority(priorityTranslations, translationVariant, translationDetails.TranslationEntryId))
                    result++;
                if (translationVariant.Synonyms == null)
                    continue;

                foreach (var synonym in translationVariant.Synonyms)
                    if (MarkPriority(priorityTranslations, synonym, translationDetails.TranslationEntryId))
                        result++;
            }

            return result;
        }

        private bool MarkPriority([NotNull] ICollection<ExchangeWord> priorityTranslations, [NotNull] Word word, [NotNull] object translationEntryId)
        {
            if (!priorityTranslations.Contains(word, _wordsEqualityComparer) || _wordPriorityRepository.IsPriority(word, translationEntryId))
                return false;

            _wordPriorityRepository.MarkPriority(word, translationEntryId);
            _messenger.Send(_viewModelAdapter.Adapt<PriorityWordViewModel>(word), MessengerTokens.PriorityChangeToken);
            return true;
        }

        private void OnProgress(int current, int total)
        {
            Progress?.Invoke(this, new ProgressEventArgs(current, total));
        }
    }
}