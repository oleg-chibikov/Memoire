using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mémoire.Contracts.DAL.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mémoire.Core.Exchange
{
    public sealed class TranslationEntryContractResolver : DefaultContractResolver
    {
        static readonly IDictionary<Type, string[]> Excluded = new Dictionary<Type, string[]>
        {
            {
                typeof(LearningInfo), new[]
                {
                    nameof(LearningInfo.Id)
                }
            }
        };

        Type? _currentDeclaringType;

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            _currentDeclaringType = type;
            return base.CreateProperties(type, memberSerialization);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var objectType = _currentDeclaringType;
            property.ShouldSerialize = _ => !ShouldExclude(member, objectType);
            return property;
        }

        static bool ShouldExclude(MemberInfo memberInfo, Type? objectType)
        {
            if (objectType == null)
            {
                return false;
            }

            Excluded.TryGetValue(objectType, out var excludedProperties);
            return excludedProperties?.Any(x => x == memberInfo.Name) == true;
        }
    }
}
