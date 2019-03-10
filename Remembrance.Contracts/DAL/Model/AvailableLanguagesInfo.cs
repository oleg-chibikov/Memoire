using System.Collections.Generic;
using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class AvailableLanguagesInfo
    {
        [NotNull]
        public IReadOnlyDictionary<string, HashSet<string>> Directions { get; set; }

        [NotNull]
        public IReadOnlyDictionary<string, string> Languages { get; set; }
    }
}