// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Contracts.ImageSearch.Data
{
    public sealed class ImageInfo
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string Name { get; set; }

#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "String is OK here")]
        public string ThumbnailUrl { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "String is OK here")]
        public string Url { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
