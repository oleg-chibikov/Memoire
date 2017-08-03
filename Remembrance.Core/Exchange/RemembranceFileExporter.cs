using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Card.Management.CardManagement;
using Remembrance.Card.Management.CardManagement.Data;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Card.Management.Exchange
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
        private readonly IWordsProcessor _wordsProcessor;

        public RemembranceFileExporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ILog logger,
            [NotNull] IWordsProcessor wordsProcessor)
        {
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
                        translationEntry.Translations = new PriorityWord[0];
                        var translationInfo = _wordsProcessor.ReloadTranslationDetailsIfNeeded(translationEntry);
                        var priorityWordsIds = new HashSet<string>();
                        foreach (var translationVariant in translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
                        {
                            if (translationVariant.IsPriority)
                                priorityWordsIds.Add(translationVariant.Text);
                            if (translationVariant.Synonyms == null)
                                continue;

                            foreach (var synonym in translationVariant.Synonyms.Where(synonym => synonym.IsPriority))
                                priorityWordsIds.Add(synonym.Text);
                        }

                        exportEntries.Add(
                            new RemembranceExchangeEntry(
                                priorityWordsIds.Any()
                                    ? priorityWordsIds
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