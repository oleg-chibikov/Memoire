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
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Core.CardManagement.Data;
using Remembrance.ViewModel.Translation;
using Scar.Common.Events;
using Scar.Common.Exceptions;
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
            [NotNull] IMessageHub messenger,
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
            var existingTranslationEntries = _translationEntryRepository.GetAll()
                .ToDictionary(x => x.Key, x => x);
            var totalCount = deserialized.Length;
            var errorsList = new List<string>();
            var count = 0;
            await deserialized.RunByBlocksAsync(
                    MaxBlockSize,
                    async (exchangeEntriesBlock, index, blocksCount) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _logger.Trace($"Processing block {index} out of {blocksCount} ({exchangeEntriesBlock.Length} files)...");
                        var importTasks = exchangeEntriesBlock.Select(
                            async exchangeEntry =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                _logger.Info($"Importing from {exchangeEntry.Text}...");
                                try
                                {
                                    var changed = false;
                                    var key = await GetKeyAsync(exchangeEntry, cancellationToken)
                                        .ConfigureAwait(false);
                                    var priorityTranslations = GetPriorityTranslations(exchangeEntry);
                                    TranslationEntry translationEntry = null;
                                    TranslationInfo translationInfo = null;
                                    if (existingTranslationEntries.ContainsKey(key))
                                    {
                                        translationEntry = existingTranslationEntries[key];
                                    }

                                    if (translationEntry == null)
                                    {
                                        //TODO: Check manual tran changed. If so then set Changed=true;
                                        var manualTranslations = GetManualTranslations(exchangeEntry);
                                        translationInfo = await WordsProcessor.AddOrChangeWordAsync(key.Text, cancellationToken, key.SourceLanguage, key.TargetLanguage, null, false, null, manualTranslations)
                                            .ConfigureAwait(false);
                                        translationEntry = translationInfo.TranslationEntry;
                                        changed = true;
                                    }

                                    //TODO: See inside
                                    var learningInfoUpdated = UpdateLearningInfo(exchangeEntry, translationEntry);
                                    var priorityTranslationsUpdated = await UpdatePrioirityTranslationsAsync(cancellationToken, priorityTranslations, translationInfo, translationEntry)
                                        .ConfigureAwait(false);
                                    changed |= learningInfoUpdated;
                                    changed |= priorityTranslationsUpdated;
                                    return changed
                                        ? translationInfo
                                        : null;
                                }
                                catch (LocalizableException ex)
                                {
                                    _messenger.Publish($"Cannot translate {exchangeEntry.Text}. The word is skipped".ToError(ex));
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
                        var blockResult = await Task.WhenAll(importTasks)
                            .ConfigureAwait(false);

                        if (blockResult.Any())
                        {
                            _messenger.Publish(
                                blockResult.Where(x => x != null)
                                    .ToArray());
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
                    var translationDetails = await WordsProcessor.ReloadTranslationDetailsIfNeededAsync(
                            translationEntry.Id,
                            translationEntry.Key.Text,
                            translationEntry.Key.SourceLanguage,
                            translationEntry.Key.TargetLanguage,
                            translationEntry.ManualTranslations,
                            cancellationToken)
                        .ConfigureAwait(false);
                    translationInfo = new TranslationInfo(translationEntry, translationDetails);
                }

                var importedPriorityWordsCount = ImportPriority(priorityTranslations, translationInfo);
                if (importedPriorityWordsCount != 0)
                {
                    _logger.Trace($"Imported {importedPriorityWordsCount} priority words for {translationInfo}");
                    return true;
                }

                _logger.Trace($"No priority words were imported for {translationInfo}");
                return false;
            }

            return false;
        }

        private bool UpdateLearningInfo([NotNull] T exchangeEntry, [NotNull] TranslationEntry translationEntry)
        {
            var learningInfoChanged = SetLearningInfo(exchangeEntry, translationEntry);
            if (learningInfoChanged)
            {
                _translationEntryRepository.Save(translationEntry);
            }

            return learningInfoChanged;
        }

        [ItemNotNull]
        protected abstract Task<TranslationEntryKey> GetKeyAsync([NotNull] T exchangeEntry, CancellationToken cancellationToken);

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
                if (MarkPriority(priorityTranslations, translationVariant, translationInfo.TranslationEntry.Id, translationInfo.TranslationEntry.Key.TargetLanguage))
                {
                    result++;
                }

                if (translationVariant.Synonyms == null)
                {
                    continue;
                }

                result += translationVariant.Synonyms.Count(synonym => MarkPriority(priorityTranslations, synonym, translationInfo.TranslationEntry.Id, translationInfo.TranslationEntry.Key.TargetLanguage));
            }

            return result;
        }

        private bool MarkPriority([NotNull] ICollection<ExchangeWord> priorityTranslations, [NotNull] Word word, [NotNull] object translationEntryId, string targetLanguage)
        {
            if (!priorityTranslations.Contains(word, _wordsEqualityComparer) || _wordPriorityRepository.IsPriority(word, translationEntryId))
            {
                return false;
            }

            _wordPriorityRepository.MarkPriority(word, translationEntryId);
            var priorityWordViewModel = _viewModelAdapter.Adapt<PriorityWordViewModel>(word);
            priorityWordViewModel.SetIsPriority(true);
            _messenger.Publish(priorityWordViewModel);
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
                var array = objArray.Skip(index * maxBlockSize)
                    .Take(maxBlockSize)
                    .ToArray();
                if (array.Length == 0 || !await action(array, index, num))
                {
                    break;
                }
            }
        }
    }
}