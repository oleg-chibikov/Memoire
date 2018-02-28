using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Exchange;
using Remembrance.Contracts.Exchange.Data;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Core.CardManagement.Data;
using Remembrance.Resources;
using Scar.Common;
using Scar.Common.Events;
using Scar.Common.Messages;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal abstract class BaseFileImporter<T> : IFileImporter
        where T : IExchangeEntry
    {
        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        private const int MaxBlockSize = 25;

        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        protected BaseFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] ILearningInfoRepository learningInfoRepository)
        {
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
        }

        public event EventHandler<ProgressEventArgs> Progress;

        [NotNull]
        public async Task<ExchangeResult> ImportAsync(string fileName, CancellationToken cancellationToken)
        {
            // TODO: Split into sub methods
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            T[] deserialized;

            try
            {
                var file = File.ReadAllText(fileName);
                deserialized = JsonConvert.DeserializeObject<T[]>(file);
            }
            catch (IOException ex)
            {
                _logger.Warn("Cannot load file from disk", ex);
                return new ExchangeResult(false, null, 0);
            }
            catch (Exception ex)
            {
                _logger.Warn("Cannot deserialize object", ex);
                return new ExchangeResult(false, null, 0);
            }

            _logger.Trace("Getting all translations...");
            var existingTranslationEntries = _translationEntryRepository.GetAll().ToDictionary(translationEntry => translationEntry.Id, translationEntry => translationEntry);
            var totalCount = deserialized.Length;
            var errorsList = new List<string>();
            var count = 0;
            await deserialized.RunByBlocksAsync(
                    MaxBlockSize,
                    async (exchangeEntriesBlock, index, blocksCount) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _logger.TraceFormat("Processing block {0} out of {1} ({2} files)...", index, blocksCount, exchangeEntriesBlock.Length);
                        var importTasks = exchangeEntriesBlock.Select(
                            async exchangeEntry =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                _logger.TraceFormat("Importing {0}...", exchangeEntry.Text);
                                try
                                {
                                    var changed = false;
                                    var translationEntryKey = await GetTranslationEntryKeyAsync(exchangeEntry, cancellationToken).ConfigureAwait(false);
                                    TranslationEntry translationEntry = null;
                                    if (existingTranslationEntries.ContainsKey(translationEntryKey))
                                    {
                                        translationEntry = existingTranslationEntries[translationEntryKey];
                                    }

                                    var manualTranslationsUpdated = false;
                                    LearningInfo learningInfo;

                                    var manualTranslations = GetManualTranslations(exchangeEntry);
                                    if (translationEntry == null)
                                    {
                                        var translationInfo = await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(
                                                                      new TranslationEntryAdditionInfo(translationEntryKey.Text, translationEntryKey.SourceLanguage, translationEntryKey.TargetLanguage),
                                                                      cancellationToken,
                                                                      null,
                                                                      false,
                                                                      manualTranslations)
                                                                  .ConfigureAwait(false);
                                        translationEntry = translationInfo.TranslationEntry;
                                        changed = true;
                                        learningInfo = translationInfo.LearningInfo;
                                    }
                                    else
                                    {
                                        learningInfo = _learningInfoRepository.GetOrInsert(translationEntryKey);
                                        if (manualTranslations != null)
                                        {
                                            if (translationEntry.ManualTranslations?.SequenceEqual(manualTranslations) != true)
                                            {
                                                translationEntry.ManualTranslations = manualTranslations;
                                                manualTranslationsUpdated = true;
                                            }
                                        }
                                    }

                                    var learningInfoUpdated = UpdateLearningInfo(exchangeEntry, learningInfo);
                                    var priorityTranslationsUpdated = UpdatePrioirityTranslationsAsync(exchangeEntry, translationEntry);
                                    changed |= manualTranslationsUpdated;
                                    changed |= priorityTranslationsUpdated;
                                    _logger.InfoFormat("Imported {0}", exchangeEntry.Text);
                                    if (changed)
                                    {
                                        _translationEntryRepository.Update(translationEntry);
                                    }

                                    if (learningInfoUpdated)
                                    {
                                        _learningInfoRepository.Update(learningInfo);
                                    }

                                    changed |= learningInfoUpdated;
                                    return changed ? translationEntry : null;
                                }
                                catch (Exception ex)
                                {
                                    _messenger.Publish(string.Format(Errors.CannotImportWord, exchangeEntry.Text).ToError(ex));
                                    lock (errorsList)
                                    {
                                        errorsList.Add(exchangeEntry.Text);
                                    }

                                    return null;
                                }
                                finally
                                {
                                    OnProgress(Interlocked.Increment(ref count), totalCount);
                                }
                            });
                        var blockResult = await Task.WhenAll(importTasks).ConfigureAwait(false);

                        if (blockResult.Any())
                        {
                            _messenger.Publish(blockResult.Where(x => x != null).ToArray());
                        }

                        return true;
                    })
                .ConfigureAwait(false);

            return new ExchangeResult(true, errorsList.Any() ? errorsList.ToArray() : null, count);
        }

        [CanBeNull]
        protected virtual ICollection<ManualTranslation> GetManualTranslations([NotNull] T exchangeEntry)
        {
            return null;
        }

        [CanBeNull]
        protected abstract ICollection<BaseWord> GetPriorityTranslations([NotNull] T exchangeEntry);

        [ItemNotNull]
        [NotNull]
        protected abstract Task<TranslationEntryKey> GetTranslationEntryKeyAsync([NotNull] T exchangeEntry, CancellationToken cancellationToken);

        protected abstract bool UpdateLearningInfo([NotNull] T exchangeEntry, [NotNull] LearningInfo learningInfo);

        private void OnProgress(int current, int total)
        {
            Progress?.Invoke(this, new ProgressEventArgs(current, total));
        }

        private void PublishPriorityWord([NotNull] TranslationEntry translationEntry, [NotNull] BaseWord priorityTranslation)
        {
            _logger.InfoFormat("Imported priority translation {0} for {1}", priorityTranslation, translationEntry);
            var priorityWordKey = new PriorityWordKey(true, new WordKey(translationEntry.Id, priorityTranslation));
            _messenger.Publish(priorityWordKey);
        }

        private bool UpdatePrioirityTranslationsAsync([NotNull] T exchangeEntry, [NotNull] TranslationEntry translationEntry)
        {
            var priorityTranslations = GetPriorityTranslations(exchangeEntry);
            if (priorityTranslations?.Any() != true)
            {
                return false;
            }

            var result = false;
            if (translationEntry.PriorityWords == null)
            {
                translationEntry.PriorityWords = new HashSet<BaseWord>(priorityTranslations);
                foreach (var priorityTranslation in priorityTranslations)
                {
                    PublishPriorityWord(translationEntry, priorityTranslation);
                }

                result = true;
            }
            else
            {
                foreach (var priorityTranslation in priorityTranslations.Where(priorityTranslation => !translationEntry.PriorityWords.Contains(priorityTranslation)))
                {
                    translationEntry.PriorityWords.Add(priorityTranslation);
                    PublishPriorityWord(translationEntry, priorityTranslation);
                    result = true;
                }
            }

            return result;
        }
    }
}