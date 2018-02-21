using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationEntryDeletion : TrackedEntity<TranslationEntryKey>
    {
        [UsedImplicitly]
        public TranslationEntryDeletion()
        {
        }

        public TranslationEntryDeletion([NotNull] TranslationEntryKey key)
        {
            Id = key;
        }

        public override string ToString()
        {
            return $"{Id}";
        }
    }
}