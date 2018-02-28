using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.ImageSearch.Data
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class ImageInfoWithBitmap
    {
        [CanBeNull]
        public byte[] ImageBitmap { get; set; }

        [NotNull]
        public ImageInfo ImageInfo { get; set; }

        [CanBeNull]
        public byte[] ThumbnailBitmap { get; set; }
    }
}