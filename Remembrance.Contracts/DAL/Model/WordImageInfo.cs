using System;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch.Data;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class WordImageInfo : Entity<WordKey>
    {
        [UsedImplicitly]
        public WordImageInfo()
        {
        }

        public WordImageInfo([NotNull] WordKey wordKey, [CanBeNull] ImageInfoWithBitmap image, int?[] nonAvailableIndexes)
        {
            Id = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            Image = image;
            NonAvailableIndexes = nonAvailableIndexes;
        }

        [CanBeNull]
        public ImageInfoWithBitmap Image { get; set; }

        [CanBeNull]
        public int?[] NonAvailableIndexes { get; set; }

        public override string ToString()
        {
            return $"Image for {Id})";
        }
    }
}