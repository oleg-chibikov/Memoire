using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.Card.Management.Data
{
    [UsedImplicitly]
    internal class ExportEntry : IExportEntry
    {
        public ExportEntry([CanBeNull] HashSet<string> priorityTranslations, [NotNull] TranslationEntry translationEntry)
        {
            if (translationEntry == null)
                throw new ArgumentNullException(nameof(translationEntry));
            PriorityTranslations = priorityTranslations;
            TranslationEntry = translationEntry;
        }

        [CanBeNull, JsonProperty("PriorityTranslations")]
        public HashSet<string> PriorityTranslations { get; }

        [NotNull, JsonProperty("TranslationEntry")]
        public TranslationEntry TranslationEntry { get; }

        public string Text => TranslationEntry.Key.Text;
    }
}