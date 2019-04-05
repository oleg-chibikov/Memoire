

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.ImageSearch.Data
{
    public sealed class ImageInfoWithBitmap
    {
        public byte[]? ImageBitmap { get; set; }

        public ImageInfo ImageInfo { get; set; }

        public byte[]? ThumbnailBitmap { get; set; }
    }
}