using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class DetectionResult
    {
        [NotNull]
        public string Code { get; set; }

        [NotNull]
        public string Language { get; set; }
    }
}