using JetBrains.Annotations;

namespace Remembrance.Contracts.ImageSearch.Data
{
    public sealed class ImageInfoWithBitmap
    {
        [NotNull]
        public ImageInfo ImageInfo { get; set; }

        [CanBeNull]
        public byte[] ImageBitmap { get; set; }

        [CanBeNull]
        public byte[] ThumbnailBitmap { get; set; }
    }
}