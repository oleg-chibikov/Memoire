// ReSharper disable NotNullMemberIsNotInitialized

using System.Collections.Generic;

namespace Remembrance.Contracts.ImageSearch.Data
{
    public sealed class ImageInfoWithBitmap
    {
        public IReadOnlyCollection<byte>? ImageBitmap { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public ImageInfo ImageInfo { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public IReadOnlyCollection<byte>? ThumbnailBitmap { get; set; }
    }
}
