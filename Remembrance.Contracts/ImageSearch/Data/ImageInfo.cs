using JetBrains.Annotations;

namespace Remembrance.Contracts.ImageSearch.Data
{
    public class ImageInfo
    {
        [NotNull]
        public string Url { get; set; }

        [NotNull]
        public string ThumbnailUrl { get; set; }

        [NotNull]
        public string Name { get; set; }
    }
}