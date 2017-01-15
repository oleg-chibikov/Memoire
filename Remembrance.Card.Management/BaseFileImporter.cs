using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.Management.Data;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Scar.Common.Exceptions;

// ReSharper disable MemberCanBePrivate.Global

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal abstract class BaseFileImporter<T> : IFileImporter
        where T : IExportEntry
    {
        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly IMessenger Messenger;

        [NotNull]
        protected readonly ITranslationDetailsRepository TranslationDetailsRepository;

        [NotNull]
        protected readonly ITranslationEntryRepository TranslationEntryRepository;

        [NotNull]
        protected readonly IWordsAdder WordsAdder;

        public BaseFileImporter(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILog logger,
            [NotNull] IWordsAdder wordsAdder, [NotNull] IMessenger messenger,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository)
        {
            if (translationEntryRepository == null)
                throw new ArgumentNullException(nameof(translationEntryRepository));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (wordsAdder == null)
                throw new ArgumentNullException(nameof(wordsAdder));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));
            if (translationDetailsRepository == null)
                throw new ArgumentNullException(nameof(translationDetailsRepository));

            TranslationEntryRepository = translationEntryRepository;
            Logger = logger;
            WordsAdder = wordsAdder;
            Messenger = messenger;
            TranslationDetailsRepository = translationDetailsRepository;
        }

        public bool Import(string fileName, out string[] errors, out int count)
        {
            //TODO: spinner
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            errors = null;
            var e = new List<string>();
            count = 0;
            T[] deserialized;
            try
            {
                var file = File.ReadAllText(fileName);
                deserialized = JsonConvert.DeserializeObject<T[]>(file);
            }
            catch (IOException ex)
            {
                Logger.Warn("Cannot load file from disk", ex);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn("Cannot deserialize object", ex);
                return false;
            }
            var existing = TranslationEntryRepository.GetAll();
            var existingKeys = new HashSet<TranslationEntryKey>(existing.Select(x => x.Key));

            //TODO: in chunks
            foreach (var exportEntry in deserialized)
            {
                Logger.Info($"Importing from {exportEntry.Text}...");
                TranslationInfo translationInfo;
                try
                {
                    var key = GetKey(exportEntry);
                    if (existingKeys.Contains(key))
                        continue;
                    translationInfo = WordsAdder.AddWordWithChecks(key.Text, key.SourceLanguage, key.TargetLanguage);
                }
                catch (LocalizableException ex)
                {
                    Logger.Warn($"Cannot translate {exportEntry.Text}. The word is skipped", ex);
                    e.Add(exportEntry.Text);
                    continue;
                }
                var priorityTranslations = GetPriorityTranslations(exportEntry);
                if (priorityTranslations != null)
                {
                    foreach (var partOfSpeechTranslation in translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations)
                    foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                    {
                        AddOrRemoveTranslation(priorityTranslations, translationVariant, translationInfo.TranslationEntry.Translations);
                        if (translationVariant.Synonyms == null)
                            continue;
                        foreach (var synonym in translationVariant.Synonyms)
                            AddOrRemoveTranslation(priorityTranslations, synonym, translationInfo.TranslationEntry.Translations);
                    }
                    if (!translationInfo.TranslationEntry.Translations.Any())
                        translationInfo.TranslationEntry.Translations = translationInfo.TranslationDetails.TranslationResult.GetDefaultWords();
                    TranslationDetailsRepository.Save(translationInfo.TranslationDetails);
                }
                Messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
                count++;
            }
            if (e.Any())
                errors = e.ToArray();
            return true;
        }

        protected abstract TranslationEntryKey GetKey([NotNull] T exportEntry);
        protected abstract ICollection<string> GetPriorityTranslations([NotNull] T exportEntry);

        private static void AddOrRemoveTranslation([NotNull] ICollection<string> priorityTranslations, [NotNull] PriorityWord word, [NotNull] ICollection<PriorityWord> translations)
        {
            if (priorityTranslations.Contains(word.Text, StringComparer.InvariantCultureIgnoreCase))
            {
                word.IsPriority = true;
                if (translations.All(x => x.Text != word.Text))
                    translations.Add(word);
            }
            else
            {
                var matchingToRemove = translations.Where(x => x.Text == word.Text).ToList();
                foreach (var toRemove in matchingToRemove)
                    translations.Remove(toRemove);
            }
        }
    }
}