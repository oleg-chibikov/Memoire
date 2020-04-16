using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Core.Exchange
{
    sealed class TranslationEntryContractResolver : DefaultContractResolver
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

        bool ShouldExclude(MemberInfo memberInfo, Type? objectType)
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
