using System;
using System.Collections.Generic;
using Remembrance.Contracts.ImageSearch.Data;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordImageInfo : Entity<WordKey>
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public WordImageInfo()
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        {
        }

        public WordImageInfo(WordKey wordKey, ImageInfoWithBitmap? image, IReadOnlyCollection<int?> nonAvailableIndexes)
        {
            Id = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            Image = image;
            NonAvailableIndexes = nonAvailableIndexes ?? throw new ArgumentNullException(nameof(nonAvailableIndexes));
        }

        public ImageInfoWithBitmap? Image { get; set; }

        public IReadOnlyCollection<int?> NonAvailableIndexes { get; set; }

        public override string ToString()
        {
            return $"Image for {Id})";
        }
    }
}
