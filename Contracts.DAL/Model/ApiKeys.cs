using System;

namespace Mémoire.Contracts.DAL.Model
{
    public class ApiKeys : IEquatable<ApiKeys>
    {
        public string YandexTranslation { get; set; }

        public string YandexTextToSpeech { get; set; }

        public string YandexPredictor { get; set; }

        public string UClassify { get; set; }

        public static ApiKeys CreateDefault() =>
            new ApiKeys
            {
                UClassify = "UDZpCiVwonVZ",
                YandexPredictor = "pdct.1.1.20171122T051204Z.8396a3f853a4f983.d577c3600f945d68cb065c86eec96a17a4648974",
                YandexTextToSpeech = "e07b8971-5fcd-477a-b141-c8620e7f06eb",
                YandexTranslation = "trnsl.1.1.20161020T065625Z.64271b9d8574b3fd.8d8ec77215125af49b964ca6d45c198666b7c176"
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

            return string.Equals(YandexTranslation, other.YandexTranslation, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(YandexTextToSpeech, other.YandexTextToSpeech, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(YandexPredictor, other.YandexPredictor, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(UClassify, other.UClassify, StringComparison.OrdinalIgnoreCase);
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
                var hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(YandexTranslation);
                hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(YandexTextToSpeech);
                hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(YandexPredictor);
                hashCode = (hashCode * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(UClassify);
                return hashCode;
            }
        }
    }
}
