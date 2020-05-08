using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Scar.Common.DAL.Contracts.Model;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class TranslationEntry : TrackedEntity<TranslationEntryKey>
    {
        public TranslationEntry()
        {
        }

        public TranslationEntry(TranslationEntryKey key)
        {
            Id = key;
        }

        public IReadOnlyCollection<ManualTranslation>? ManualTranslations { get; set; }

        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "It is initialized by LiteDB and needs setter")]
        public ISet<BaseWord>? PriorityWords { get; set; }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
