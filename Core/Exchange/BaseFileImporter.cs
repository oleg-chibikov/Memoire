using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Exchange;
using Mémoire.Contracts.Exchange.Data;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Core.CardManagement.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scar.Common;
using Scar.Common.Events;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Core.Exchange
{
    public abstract class BaseFileImporter<T> : IFileImporter
        where T : IExchangeEntry
    {
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly ILogger _logger;
        readonly IMessageHub _messenger;
        readonly ITranslationEntryProcessor _translationEntryProcessor;
        readonly ITranslationEntryRepository _translationEntryRepository;

        protected BaseFileImporter(
            ITranslationEntryRepository translationEntryRepository,
            ILogger<BaseFileImporter<T>> logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messenger,
            ILearningInfoRepository learningInfoRepository)
        {
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
        }

        public event EventHandler<ProgressEventArgs>? Progress;

        public async Task<ExchangeResult> ImportAsync(string fileName, CancellationToken cancellationToken)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
            var deserialized = Deserialize(fileName);

            if (deserialized == null)
            {
                return new ExchangeResult(false, null, 0);
            }

            var existingTranslationEntries = _translationEntryRepository.GetAll().ToDictionary(translationEntry => translationEntry.Id, translationEntry => translationEntry);
            var totalCount = deserialized.Count;
            var errorsList = new List<string>();
            var count = 0;
            var successCount = 0;
            await deserialized.RunByBlocksAsync(
                    25,
                    async (exchangeEntriesBlock, index, blockCount) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _logger.LogTrace("Processing block {Index} out of {BlockCount} ({FileCount} files)...", index, blockCount, exchangeEntriesBlock.Length);
                        var importTasks = exchangeEntriesBlock.Select(
                            async exchangeEntry =>
                            {
                                var translationEntry = await ImportOneEntryAsync(exchangeEntry, existingTranslationEntries, errorsList, cancellationToken).ConfigureAwait(false);
                                if (translationEntry != null)
                                {
                                    Interlocked.Increment(ref successCount);
                                }

                                OnProgress(Interlocked.Increment(ref count), totalCount);
                                return translationEntry;
                            });
                        var blockResult = await Task.WhenAll(importTasks).ConfigureAwait(false);

                        if (blockResult.Length > 0)
                        {
                            _messenger.Publish(blockResult.Where(x => x != null).ToArray());
                        }

                        return true;
                    })
                .ConfigureAwait(false);

            return new ExchangeResult(true, errorsList.Count > 0 ? errorsList : null, successCount);
        }

        protected virtual IReadOnlyCollection<ManualTranslation>? GetManualTranslations(T exchangeEntry)
        {
            return null;
        }

        protected abstract IReadOnlyCollection<BaseWord>? GetPriorityTranslations(T exchangeEntry);

        protected abstract Task<TranslationEntryKey> GetTranslationEntryKeyAsync(T exchangeEntry, CancellationToken cancellationToken);

        protected abstract bool UpdateLearningInfo(T exchangeEntry, LearningInfo learningInfo);

        static bool UpdateManualTranslations(IReadOnlyCollection<ManualTranslation>? manualTranslations, TranslationEntry translationEntry)
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

        IReadOnlyCollection<T>? Deserialize(string fileName)
        {
            IReadOnlyCollection<T>? deserialized = null;

            try
            {
                var file = File.ReadAllText(fileName);
                deserialized = JsonConvert.DeserializeObject<IReadOnlyCollection<T>>(file);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Cannot load file from disk");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Cannot deserialize object");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Cannot process argument");
            }

            return deserialized;
        }

        async Task<TranslationInfo?> ImportNewEntryAsync(TranslationEntryKey translationEntryKey, IReadOnlyCollection<ManualTranslation>? manualTranslations, CancellationToken cancellationToken)
        {
            return await _translationEntryProcessor.AddOrUpdateTranslationEntryAsync(
                    new TranslationEntryAdditionInfo(translationEntryKey.Text, translationEntryKey.SourceLanguage, translationEntryKey.TargetLanguage),
                    null,
                    false,
                    false,
                    manualTranslations,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        async Task<TranslationEntry?> ImportOneEntryAsync(
            T exchangeEntry,
            IDictionary<TranslationEntryKey, TranslationEntry> existingTranslationEntries,
            List<string> errorsList,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogTrace("Importing {ExchangeText}...", exchangeEntry.Text);
            var translationEntryKey = await GetTranslationEntryKeyAsync(exchangeEntry, cancellationToken).ConfigureAwait(false);
            TranslationEntry? translationEntry = null;
            if (existingTranslationEntries.TryGetValue(translationEntryKey, out var entry))
            {
                translationEntry = entry;
            }

            var manualTranslations = GetManualTranslations(exchangeEntry);
            LearningInfo learningInfo;
            var changed = false;
            if (translationEntry == null)
            {
                var translationInfo = await ImportNewEntryAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);
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
            var priorityTranslationsUpdated = UpdatePriorityTranslations(exchangeEntry, translationEntry);
            changed |= priorityTranslationsUpdated;
            _logger.LogInformation("Imported {ExchangeText}", exchangeEntry.Text);
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

        void OnProgress(int current, int total)
        {
            Progress?.Invoke(this, new ProgressEventArgs(current, total));
        }

        void PublishPriorityWord(TranslationEntry translationEntry, BaseWord priorityTranslation)
        {
            _logger.LogInformation("Imported priority translation {PriorityTranslation} for {Translation}", priorityTranslation, translationEntry);
            var priorityWordKey = new PriorityWordKey(true, new WordKey(translationEntry.Id, priorityTranslation));
            _messenger.Publish(priorityWordKey);
        }

        bool UpdatePriorityTranslations(T exchangeEntry, TranslationEntry translationEntry)
        {
            var priorityTranslations = GetPriorityTranslations(exchangeEntry);
            if (priorityTranslations?.Any() != true)
            {
                return false;
            }

            var result = false;
            var priorityWords = translationEntry.PriorityWords;
            if (priorityWords == null)
            {
                priorityWords = new HashSet<BaseWord>(priorityTranslations);
                translationEntry.PriorityWords = priorityWords;
                foreach (var priorityTranslation in priorityTranslations)
                {
                    PublishPriorityWord(translationEntry, priorityTranslation);
                }

                result = true;
            }
            else
            {
                foreach (var priorityTranslation in priorityTranslations.Where(priorityTranslation => !priorityWords.Contains(priorityTranslation)))
                {
                    priorityWords.Add(priorityTranslation);
                    PublishPriorityWord(translationEntry, priorityTranslation);
                    result = true;
                }
            }

            return result;
        }
    }
}
