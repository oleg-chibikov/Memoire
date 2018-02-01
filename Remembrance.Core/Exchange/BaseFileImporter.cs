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
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Exchange;
using Remembrance.Core.CardManagement.Data;
using Remembrance.Resources;
using Scar.Common.Events;
using Scar.Common.Messages;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal abstract class BaseFileImporter<T> : IFileImporter
        where T : IExchangeEntry
    {
        private const int MaxBlockSize = 25;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        protected BaseFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messenger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
        {
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public event EventHandler<ProgressEventArgs> Progress;

        public async Task<ExchangeResult> ImportAsync(string fileName, CancellationToken cancellationToken)
        {
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
                                _logger.TraceFormat("Importing from {0}...", exchangeEntry.Text);
                                try
                                {
                                    var changed = false;
                                    var translationEntryKey = await GetTranslationEntryKeyAsync(exchangeEntry, cancellationToken).ConfigureAwait(false);
                                    var priorityTranslations = GetPriorityTranslations(exchangeEntry);
                                    TranslationEntry translationEntry = null;
                                    TranslationInfo translationInfo = null;
                                    if (existingTranslationEntries.ContainsKey(translationEntryKey))
                                    {
                                        translationEntry = existingTranslationEntries[translationEntryKey];
                                    }

                                    var manualTranslationsUpdated = false;

                                    if (translationEntry == null)
                                    {
                                        var manualTranslations = GetManualTranslations(exchangeEntry);
                                        translationInfo = await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(
                                                new TranslationEntryAdditionInfo(translationEntryKey.Text, translationEntryKey.SourceLanguage, translationEntryKey.TargetLanguage),
                                                cancellationToken,
                                                null,
                                                false,
                                                manualTranslations)
                                            .ConfigureAwait(false);
                                        translationEntry = translationInfo.TranslationEntry;
                                        changed = true;
                                    }
                                    else
                                    {
                                        var manualTranslations = GetManualTranslations(exchangeEntry);
                                        if (manualTranslations != null)
                                        {
                                            if (translationEntry.ManualTranslations?.SequenceEqual(manualTranslations) != true)
                                            {
                                                translationEntry.ManualTranslations = manualTranslations;
                                                manualTranslationsUpdated = true;
                                            }
                                        }
                                    }

                                    //TODO: See inside
                                    var learningInfoUpdated = SetLearningInfo(exchangeEntry, translationEntry);
                                    if (manualTranslationsUpdated || learningInfoUpdated)
                                    {
                                        _translationEntryRepository.Update(translationEntry);
                                    }

                                    var priorityTranslationsUpdated = await UpdatePrioirityTranslationsAsync(cancellationToken, priorityTranslations, translationInfo, translationEntry).ConfigureAwait(false);
                                    changed |= manualTranslationsUpdated;
                                    changed |= learningInfoUpdated;
                                    changed |= priorityTranslationsUpdated;
                                    _logger.InfoFormat("Imported from {0}", exchangeEntry.Text);
                                    return changed
                                        ? translationInfo
                                        : null;
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

            return new ExchangeResult(
                true,
                errorsList.Any()
                    ? errorsList.ToArray()
                    : null,
                count);
        }

        private async Task<bool> UpdatePrioirityTranslationsAsync(
            CancellationToken cancellationToken,
            [CanBeNull] ICollection<ExchangeWord> priorityTranslations,
            [CanBeNull] TranslationInfo translationInfo,
            [NotNull] TranslationEntry translationEntry)
        {
            if (priorityTranslations != null)
            {
                if (translationInfo == null)
                {
                    var translationDetails = await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, cancellationToken).ConfigureAwait(false);
                    translationInfo = new TranslationInfo(translationEntry, translationDetails);
                }

                var importedPriorityWordsCount = ImportPriority(priorityTranslations, translationInfo);
                if (importedPriorityWordsCount != 0)
                {
                    _logger.InfoFormat("Imported {0} priority words for {1}", importedPriorityWordsCount, translationInfo);
                    return true;
                }

                _logger.TraceFormat("No priority words were imported for {0}", translationInfo);
                return false;
            }

            return false;
        }

        [ItemNotNull]
        protected abstract Task<TranslationEntryKey> GetTranslationEntryKeyAsync([NotNull] T exchangeEntry, CancellationToken cancellationToken);

        [CanBeNull]
        protected virtual ManualTranslation[] GetManualTranslations([NotNull] T exchangeEntry)
        {
            return null;
        }

        [CanBeNull]
        protected abstract ICollection<ExchangeWord> GetPriorityTranslations([NotNull] T exchangeEntry);

        protected abstract bool SetLearningInfo([NotNull] T exchangeEntry, [NotNull] TranslationEntry translationEntry);

        private int ImportPriority([NotNull] ICollection<ExchangeWord> priorityTranslations, [NotNull] TranslationInfo translationInfo)
        {
            var result = 0;
            foreach (var translationVariant in translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (MarkPriority(priorityTranslations, translationVariant, translationInfo.TranslationEntry.Id))
                {
                    result++;
                }

                if (translationVariant.Synonyms == null)
                {
                    continue;
                }

                result += translationVariant.Synonyms.Count(synonym => MarkPriority(priorityTranslations, synonym, translationInfo.TranslationEntry.Id));
            }

            return result;
        }

        private bool MarkPriority([NotNull] ICollection<ExchangeWord> priorityTranslations, [NotNull] IWord word, [NotNull] TranslationEntryKey translationEntryKey)
        {
            var wordKey = new WordKey(translationEntryKey, word);
            if (!priorityTranslations.Contains(word, _wordsEqualityComparer) || _wordPriorityRepository.Check(wordKey))
            {
                return false;
            }

            _wordPriorityRepository.Insert(new WordPriority(wordKey));
            var priorityWordKey = new PriorityWordKey(true, new WordKey(translationEntryKey, word));
            _messenger.Publish(priorityWordKey);
            return true;
        }

        private void OnProgress(int current, int total)
        {
            Progress?.Invoke(this, new ProgressEventArgs(current, total));
        }
    }

    public static class EnumerableExtensions
    {
        //TODO: Common lib

        public static async Task RunByBlocksAsync<T>([NotNull] this IEnumerable<T> items, int maxBlockSize, [NotNull] Func<T[], int, int, Task<bool>> action)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var objArray = items as T[] ?? items.ToArray();
            if (objArray.Length == 0)
            {
                return;
            }

            if (maxBlockSize <= 0)
            {
                maxBlockSize = 100;
            }

            var num = objArray.Length / maxBlockSize;
            if (objArray.Length % maxBlockSize > 0)
            {
                ++num;
            }

            for (var index = 0; index < num; ++index)
            {
                var array = objArray.Skip(index * maxBlockSize).Take(maxBlockSize).ToArray();
                if (array.Length == 0 || !await action(array, index, num))
                {
                    break;
                }
            }
        }
    }
}