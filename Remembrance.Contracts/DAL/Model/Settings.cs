using JetBrains.Annotations;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    // ReSharper disable NotNullMemberIsNotInitialized
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class Settings : TrackedEntity<string>, ISettings
    {
        [NotNull]
        public object Value { get; set; }
    }
}