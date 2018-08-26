using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
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
            return $"Deletion of {Id}";
        }
    }
}