using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Core.CardManagement;
using Remembrance.Core.CardManagement.Data;

namespace Remembrance.Core.Exchange
{
    [UsedImplicitly]
    internal sealed class RemembranceFileExporter : IFileExporter
    {
        private static readonly JsonSerializerSettings ExportEntrySerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new TranslationEntryContractResolver()
        };

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        public RemembranceFileExporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ILog logger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
        {
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExchangeResult> ExportAsync(string fileName, CancellationToken token)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            return await Task.Run(
                () =>
                {
                    var translationEntries = _translationEntryRepository.GetAll();
                    var exportEntries = new List<RemembranceExchangeEntry>(translationEntries.Length);
                    foreach (var translationEntry in translationEntries)
                    {
                        var translationDetails = _wordsProcessor.ReloadTranslationDetailsIfNeeded(translationEntry.Id, translationEntry.Key.Text, translationEntry.Key.SourceLanguage, translationEntry.Key.TargetLanguage);
                        var translationInfo = new TranslationInfo(translationEntry, translationDetails);
                        var priorityWords = new HashSet<ExchangeWord>(_wordsEqualityComparer);
                        foreach (var translationVariant in translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
                        {
                            if (_wordPriorityRepository.IsPriority(translationVariant, translationEntry.Id))
                                priorityWords.Add(new ExchangeWord(translationVariant));
                            if (translationVariant.Synonyms == null)
                                continue;

                            foreach (var synonym in translationVariant.Synonyms.Where(synonym => _wordPriorityRepository.IsPriority(synonym, translationEntry.Id)))
                                priorityWords.Add(new ExchangeWord(synonym));
                        }

                        exportEntries.Add(
                            new RemembranceExchangeEntry(
                                priorityWords.Any()
                                    ? priorityWords
                                    : null,
                                translationEntry));
                    }

                    try
                    {
                        var json = JsonConvert.SerializeObject(exportEntries, Formatting.Indented, ExportEntrySerializerSettings);
                        File.WriteAllText(fileName, json);
                        return new ExchangeResult(true, null, exportEntries.Count);
                    }
                    catch (IOException ex)
                    {
                        _logger.Warn("Cannot save file to disk", ex);
                    }
                    catch (JsonSerializationException ex)
                    {
                        _logger.Warn("Cannot serialize object", ex);
                    }

                    return new ExchangeResult(false, null, 0);
                },
                token);
        }
    }
}