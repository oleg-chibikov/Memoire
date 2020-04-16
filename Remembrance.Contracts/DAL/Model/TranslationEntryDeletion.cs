using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationEntryDeletion : TrackedEntity<TranslationEntryKey>
    {
        public TranslationEntryDeletion()
        {
        }

        public TranslationEntryDeletion(TranslationEntryKey key)
        {
            Id = key;
        }

        public override string ToString()
        {
            return $"Deletion of {Id}";
        }
    }
}
