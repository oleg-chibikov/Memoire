using System.Collections.Generic;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class TranslationEntry : TrackedEntity<TranslationEntryKey>
    {
        [UsedImplicitly]
        public TranslationEntry()
        {
        }

        public TranslationEntry([NotNull] TranslationEntryKey key)
        {
            Id = key;
        }

        [CanBeNull]
        public IReadOnlyCollection<ManualTranslation> ManualTranslations { get; set; }

        [CanBeNull]
        public ISet<BaseWord> PriorityWords { get; set; }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}