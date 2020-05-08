using System;
using System.Collections.Generic;
using Scar.Common.DAL.Contracts.Model;
using Scar.Services.Contracts.Data.ImageSearch;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class WordImageInfo : Entity<WordKey>
    {
        public WordImageInfo()
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
