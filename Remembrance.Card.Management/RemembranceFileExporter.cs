using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.Management.Data;
using Remembrance.DAL.Contracts;
using Remembrance.Translate.Contracts.Data.WordsTranslator;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal class RemembranceFileExporter : IFileExporter
    {
        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly ITranslationDetailsRepository translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository translationEntryRepository;

        public RemembranceFileExporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ILog logger)
        {
            if (translationEntryRepository == null)
                throw new ArgumentNullException(nameof(translationEntryRepository));
            if (translationDetailsRepository == null)
                throw new ArgumentNullException(nameof(translationDetailsRepository));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.translationEntryRepository = translationEntryRepository;
            this.translationDetailsRepository = translationDetailsRepository;
            this.logger = logger;
        }

        public bool Export(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            var translationEntries = translationEntryRepository.GetAll();
            var exportEntries = new List<ExportEntry>(translationEntries.Length);
            foreach (var translationEntry in translationEntries)
            {
                translationEntry.Translations = new PriorityWord[0];
                var details = translationDetailsRepository.GetById(translationEntry.Id);
                var priorityWordsIds = new HashSet<string>();
                foreach (var partOfSpeechTranslation in details.TranslationResult.PartOfSpeechTranslations)
                foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                {
                    if (translationVariant.IsPriority)
                        priorityWordsIds.Add(translationVariant.Text);
                    if (translationVariant.Synonyms == null)
                        continue;
                    foreach (var synonym in translationVariant.Synonyms)
                        if (synonym.IsPriority)
                            priorityWordsIds.Add(synonym.Text);
                }
                exportEntries.Add(new ExportEntry(priorityWordsIds.Any() ? priorityWordsIds : null, translationEntry));
            }
            try
            {
                var json = JsonConvert.SerializeObject(exportEntries);
                File.WriteAllText(fileName, json);
                return true;
            }
            catch (IOException ex)
            {
                logger.Warn("Cannot save file to disk", ex);
            }
            catch (JsonSerializationException ex)
            {
                logger.Warn("Cannot serialize object", ex);
            }
            return false;
        }
    }
}