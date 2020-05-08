using System;

namespace Mémoire.Contracts.DAL.Model
{
    public class CardProbabilitySettings : IEquatable<CardProbabilitySettings>
    {
        public int FavoritedItems { get; set; }

        public int ItemsWithSmallerShowCount { get; set; }

        public int ItemsWithLowerRepeatType { get; set; }

        public int OlderItems { get; set; }

        public static CardProbabilitySettings CreateDefault()
        {
            return new CardProbabilitySettings { FavoritedItems = 20, ItemsWithSmallerShowCount = 20, ItemsWithLowerRepeatType = 20, OlderItems = 20 };
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((CardProbabilitySettings)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FavoritedItems;
                hashCode = (hashCode * 397) ^ ItemsWithSmallerShowCount;
                hashCode = (hashCode * 397) ^ ItemsWithLowerRepeatType;
                hashCode = (hashCode * 397) ^ OlderItems;
                return hashCode;
            }
        }

        public bool Equals(CardProbabilitySettings? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return (FavoritedItems == other.FavoritedItems) &&
                   (ItemsWithSmallerShowCount == other.ItemsWithSmallerShowCount) &&
                   (ItemsWithLowerRepeatType == other.ItemsWithLowerRepeatType) &&
                   (OlderItems == other.OlderItems);
        }
    }
}
