using System;
using Newtonsoft.Json;

namespace Remembrance.Core.ImageSearch.Qwant.JsonConverters
{
    sealed class UrlConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return !(reader.Value is string valueString) ? string.Empty : valueString.StartsWith(@"//", StringComparison.OrdinalIgnoreCase) ? $"https:{valueString}" : valueString;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}
