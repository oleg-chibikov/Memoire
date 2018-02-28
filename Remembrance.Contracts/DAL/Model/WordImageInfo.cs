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

        public WordImageInfo([NotNull] WordKey wordKey, int searchIndex, [CanBeNull] ImageInfoWithBitmap image, bool isReverse, int?[] nonAvailableIndexes)
        {
            Id = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            SearchIndex = searchIndex;
            Image = image;
            IsReverse = isReverse;
            NonAvailableIndexes = nonAvailableIndexes;
        }

        [CanBeNull]
        public ImageInfoWithBitmap Image { get; set; }

        public bool IsReverse { get; set; }

        [CanBeNull]
        public int?[] NonAvailableIndexes { get; set; }

        public int SearchIndex { get; set; }

        public override string ToString()
        {
            return $"Image for {Id})";
        }
    }
}