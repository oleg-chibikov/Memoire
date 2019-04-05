using System.Collections.Generic;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
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

        public ISet<BaseWord>? PriorityWords { get; set; }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}