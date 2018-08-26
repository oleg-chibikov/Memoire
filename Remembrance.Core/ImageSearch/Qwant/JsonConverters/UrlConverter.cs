using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Remembrance.Core.ImageSearch.Qwant.JsonConverters
{
    internal sealed class UrlConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        [NotNull]
        public override object ReadJson([NotNull] JsonReader reader, Type objectType, object existingValue, [NotNull] JsonSerializer serializer)
        {
            return !(reader.Value is string valueString)
                ? string.Empty
                : (valueString.StartsWith(@"//", StringComparison.InvariantCultureIgnoreCase) ? $"https:{valueString}" : valueString);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}