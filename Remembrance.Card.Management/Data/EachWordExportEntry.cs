using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Remembrance.Card.Management.Data
{
    [UsedImplicitly]
    internal class EachWordExportEntry : IExportEntry
    {
        public EachWordExportEntry([NotNull] string text, [CanBeNull] string translation)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            Text = text;
            Translation = translation;
        }

        [CanBeNull, JsonProperty("Translation")]
        public string Translation { get; }

        [JsonProperty("Word")]
        public string Text { get; }
    }
}