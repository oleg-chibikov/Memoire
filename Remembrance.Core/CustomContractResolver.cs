using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Remembrance.Core
{
    internal abstract class CustomContractResolver : DefaultContractResolver
    {
        protected virtual IReadOnlyDictionary<Type, JsonConverter>? PropertyConverters { get; } = null;

        protected abstract IReadOnlyDictionary<string, string> PropertyMappings { get; }

        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);

            // this will only be called once and then cached
            if (PropertyConverters != null && PropertyConverters.TryGetValue(objectType, out var jsonConverter))
            {
                contract.Converter = jsonConverter;
            }

            return contract;
        }

        protected override string ResolvePropertyName([NotNull] string propertyName)
        {
            var resolved = PropertyMappings.TryGetValue(propertyName, out var resolvedName);
            return resolved ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}