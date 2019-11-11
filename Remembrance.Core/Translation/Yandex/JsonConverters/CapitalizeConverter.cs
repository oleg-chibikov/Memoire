using System;
using Newtonsoft.Json;
using Scar.Common;

namespace Remembrance.Core.Translation.Yandex.JsonConverters
{
    internal sealed class CapitalizeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return reader.Path.EndsWith(".text", StringComparison.InvariantCultureIgnoreCase) ? reader.Value?.ToString().Capitalize() : reader.Value;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}