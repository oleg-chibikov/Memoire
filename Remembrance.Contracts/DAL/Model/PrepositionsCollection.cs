using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class PrepositionsCollection
    {
        [CanBeNull]
        public ICollection<string> Texts { get; set; }

        public override string ToString()
        {
            return Texts != null ? string.Join("/", Texts) : string.Empty;
        }
    }
}