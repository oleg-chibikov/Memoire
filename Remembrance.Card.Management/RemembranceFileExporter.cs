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

        public RemembranceFileExporter([NotNull] ITranslationEntryRepository translationEntryRepository, [NotNull] ITranslationDetailsRepository translationDetailsRepository, [NotNull] ILog logger)
        {
            this.translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            this.translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Export(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            var translationEntries = translationEntryRepository.GetAll();
            var exportEntries = new List<RemembranceExchangeEntry>(translationEntries.Length);
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

                exportEntries.Add(
                    new RemembranceExchangeEntry(
                        priorityWordsIds.Any()
                            ? priorityWordsIds
                            : null,
                        translationEntry));
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