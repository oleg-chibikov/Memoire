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
using Scar.Common;
using Scar.Common.Events;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal abstract class BaseFileImporter<T> : IFileImporter
        where T : IExchangeEntry
    {
        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

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
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
        }

        public event EventHandler<ProgressEventArgs> Progress;

        [NotNull]
        public async Task<ExchangeResult> ImportAsync(string fileName, CancellationToken cancellationToken)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
            var deserialized = Deserialize(fileName);

            if (deserialized == null)
            {
                return new ExchangeResult(false, null, 0);
            }

            var existingTranslationEntries = _translationEntryRepository.GetAll().ToDictionary(translationEntry => translationEntry.Id, translationEntry => translationEntry);
            var totalCount = deserialized.Length;
            var errorsList = new List<string>();
            var count = 0;
            var successCount = 0;
            await deserialized.RunByBlocksAsync(
                    25,
                    async (exchangeEntriesBlock, index, blocksCount) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _logger.TraceFormat("Processing block {0} out of {1} ({2} files)...", index, blocksCount, exchangeEntriesBlock.Length);
                        var importTasks = exchangeEntriesBlock.Select(
                            async exchangeEntry =>
                            {
                                var translationEntry = await ImportOneEntry(cancellationToken, exchangeEntry, existingTranslationEntries, errorsList).ConfigureAwait(false);
                                if (translationEntry != null)
                                {
                                    Interlocked.Increment(ref successCount);
                                }

                                OnProgress(Interlocked.Increment(ref count), totalCount);
                                return translationEntry;
                            });
                        var blockResult = await Task.WhenAll(importTasks).ConfigureAwait(false);

                        if (blockResult.Any())
                        {
                            _messenger.Publish(blockResult.Where(x => x != null).ToArray());
                        }

                        return true;
                    })
                .ConfigureAwait(false);

            return new ExchangeResult(true, errorsList.Any() ? errorsList : null, successCount);
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

        private static bool UpdateManualTranslations([CanBeNull] ICollection<ManualTranslation> manualTranslations, [NotNull] TranslationEntry translationEntry)
        {
            if (manualTranslations == null)
            {
                return false;
            }

            if (translationEntry.ManualTranslations?.SequenceEqual(manualTranslations) == true)
            {
                return false;
            }

            translationEntry.ManualTranslations = manualTranslations;
            return true;
        }

        [CanBeNull]
        private T[] Deserialize([NotNull] string fileName)
        {
            T[] deserialized = null;

            try
            {
                var file = File.ReadAllText(fileName);
                deserialized = JsonConvert.DeserializeObject<T[]>(file);
            }
            catch (IOException ex)
            {
                _logger.Warn("Cannot load file from disk", ex);
            }
            catch (Exception ex)
            {
                _logger.Warn("Cannot deserialize object", ex);
            }

            return deserialized;
        }

        [ItemCanBeNull]
        private async Task<TranslationInfo> ImportNewEntry(
            CancellationToken cancellationToken,
            [NotNull] TranslationEntryKey translationEntryKey,
            [CanBeNull] ICollection<ManualTranslation> manualTranslations)
        {
            return await _translationEntryProcessor.AddOrUpdateTranslationEntryAsync(
                    new TranslationEntryAdditionInfo(translationEntryKey.Text, translationEntryKey.SourceLanguage, translationEntryKey.TargetLanguage),
                    cancellationToken,
                    null,
                    false,
                    manualTranslations)
                .ConfigureAwait(false);
        }

        [ItemCanBeNull]
        private async Task<TranslationEntry> ImportOneEntry(
            CancellationToken cancellationToken,
            [NotNull] T exchangeEntry,
            [NotNull] IDictionary<TranslationEntryKey, TranslationEntry> existingTranslationEntries,
            [NotNull] List<string> errorsList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.TraceFormat("Importing {0}...", exchangeEntry.Text);
            var translationEntryKey = await GetTranslationEntryKeyAsync(exchangeEntry, cancellationToken).ConfigureAwait(false);
            TranslationEntry translationEntry = null;
            if (existingTranslationEntries.ContainsKey(translationEntryKey))
            {
                translationEntry = existingTranslationEntries[translationEntryKey];
            }

            var manualTranslations = GetManualTranslations(exchangeEntry);
            LearningInfo learningInfo;
            var changed = false;
            if (translationEntry == null)
            {
                var translationInfo = await ImportNewEntry(cancellationToken, translationEntryKey, manualTranslations).ConfigureAwait(false);
                if (translationInfo == null)
                {
                    lock (errorsList)
                    {
                        errorsList.Add(exchangeEntry.Text);
                    }

                    return null;
                }

                translationEntry = translationInfo.TranslationEntry;
                learningInfo = translationInfo.LearningInfo;
                changed = true;
            }
            else
            {
                learningInfo = _learningInfoRepository.GetOrInsert(translationEntryKey);
                changed |= UpdateManualTranslations(manualTranslations, translationEntry);
            }

            var learningInfoUpdated = UpdateLearningInfo(exchangeEntry, learningInfo);
            var priorityTranslationsUpdated = UpdatePriorityTranslationsAsync(exchangeEntry, translationEntry);
            changed |= priorityTranslationsUpdated;
            _logger.InfoFormat("Imported {0}", exchangeEntry.Text);
            if (changed)
            {
                _translationEntryRepository.Update(translationEntry);
            }

            if (learningInfoUpdated)
            {
                _learningInfoRepository.Update(learningInfo);
                changed = true;
            }

            return changed ? translationEntry : null;
        }

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

        private bool UpdatePriorityTranslationsAsync([NotNull] T exchangeEntry, [NotNull] TranslationEntry translationEntry)
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