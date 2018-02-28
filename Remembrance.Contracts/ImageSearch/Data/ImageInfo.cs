using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.ImageSearch.Data
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class ImageInfo
    {
        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public string ThumbnailUrl { get; set; }

        [NotNull]
        public string Url { get; set; }
    }
}