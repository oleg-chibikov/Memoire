using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Core.Exchange
{
    internal sealed class TranslationEntryContractResolver : DefaultContractResolver
    {
        [NotNull]
        private static readonly IDictionary<Type, string[]> Excluded = new Dictionary<Type, string[]>
        {
            {
                typeof(LearningInfo), new[]
                {
                    nameof(LearningInfo.Id)
                }
            }
        };

        [CanBeNull]
        private Type _currentDeclaringType;

        [NotNull]
        protected override JsonProperty CreateProperty([NotNull] MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var objectType = _currentDeclaringType;
            property.ShouldSerialize = _ => !ShouldExclude(member, objectType);
            return property;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            _currentDeclaringType = type;
            return base.CreateProperties(type, memberSerialization);
        }

        private bool ShouldExclude([NotNull] MemberInfo memberInfo, [CanBeNull] Type objectType)
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