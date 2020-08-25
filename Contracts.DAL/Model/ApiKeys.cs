using System;

namespace Mémoire.Contracts.DAL.Model
{
    public class ApiKeys : IEquatable<ApiKeys>
    {
        public string YandexTextToSpeech { get; set; }

        public string UClassify { get; set; }

        public static ApiKeys CreateDefault() =>
            new ApiKeys
            {
                UClassify = "UDZpCiVwonVZ",
                YandexTextToSpeech = "e07b8971-5fcd-477a-b141-c8620e7f06eb"
            };

        public bool Equals(ApiKeys? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(YandexTextToSpeech, other.YandexTextToSpeech, StringComparison.OrdinalIgnoreCase) && string.Equals(UClassify, other.UClassify, StringComparison.OrdinalIgnoreCase);
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

            return Equals((ApiKeys)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(YandexTextToSpeech);
                hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(UClassify);
                return hashCode;
            }
        }
    }
}
