// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Contracts.ImageSearch.Data
{
    public sealed class ImageInfoWithBitmap
    {
        public byte[]? ImageBitmap { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public ImageInfo ImageInfo { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public byte[]? ThumbnailBitmap { get; set; }
    }
}