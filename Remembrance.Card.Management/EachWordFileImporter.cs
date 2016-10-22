using System;
using System.Collections.Generic;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.Management.Data;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Interfaces;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal class EachWordFileImporter : BaseFileImporter<EachWordExportEntry>
    {
        private static readonly char[] Separator = { ',', ';', '/', '\\' };

        [NotNull]
        private readonly ILanguageDetector languageDetector;

        public EachWordFileImporter([NotNull] ITranslationEntryRepository translationEntryRepository, [NotNull] ILog logger, [NotNull] IWordsChecker wordsChecker, [NotNull] IMessenger messenger, [NotNull] ITranslationDetailsRepository translationDetailsRepository, [NotNull] ILanguageDetector languageDetector) : base(translationEntryRepository, logger, wordsChecker, messenger, translationDetailsRepository)
        {
            if (languageDetector == null)
                throw new ArgumentNullException(nameof(languageDetector));
            this.languageDetector = languageDetector;
        }

        protected override TranslationEntryKey GetKey(EachWordExportEntry exportEntry)
        {
            var sourceLanguage = languageDetector.DetectLanguageAsync(exportEntry.Text).Result.Language ?? Constants.EnLanguage;
            var targetLanguage = WordsChecker.GetDefaultTargetLanguage(sourceLanguage);
            return new TranslationEntryKey(exportEntry.Text, sourceLanguage, targetLanguage);
        }

        protected override ICollection<string> GetPriorityTranslations(EachWordExportEntry exportEntry)
        {
            return exportEntry.Translation?.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}