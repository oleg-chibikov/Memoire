using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class DetectionResult
    {
        [CanBeNull]
        public string Code { get; set; }

        [CanBeNull]
        public string Language { get; set; }
    }
}