using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Remembrance.Translate.Yandex.ContractResolvers
{
    internal abstract class CustomContractResolver : DefaultContractResolver
    {
        protected abstract Dictionary<string, string> PropertyMappings { get; }
        protected virtual Dictionary<Type, JsonConverter> PropertyConverters { get; } = null;

        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);
            JsonConverter jsonConverter;
            // this will only be called once and then cached
            if (PropertyConverters != null && PropertyConverters.TryGetValue(objectType, out jsonConverter))
                contract.Converter = jsonConverter;
            return contract;
        }

        protected override string ResolvePropertyName([NotNull] string propertyName)
        {
            string resolvedName;
            var resolved = PropertyMappings.TryGetValue(propertyName, out resolvedName);
            return resolved
                ? resolvedName
                : base.ResolvePropertyName(propertyName);
        }
    }
}