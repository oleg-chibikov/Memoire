using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class AvailableLanguagesInfo
    {
        [NotNull]
        public IDictionary<string, HashSet<string>> Directions { get; set; }

        [NotNull]
        public IDictionary<string, string> Languages { get; set; }
    }
}