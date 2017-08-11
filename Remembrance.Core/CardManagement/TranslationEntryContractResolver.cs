using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Core.CardManagement
{
    internal sealed class TranslationEntryContractResolver : DefaultContractResolver
    {
        [NotNull]
        private static readonly Type ExcludedType = typeof(TranslationEntry);

        [NotNull]
        private static readonly string[] ExcludedProperties =
        {
            nameof(TranslationEntry.Id)
        };

        [NotNull]
        protected override JsonProperty CreateProperty([NotNull] MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = _ => !ShouldExclude(member);
            return property;
        }

        private static bool ShouldExclude([NotNull] MemberInfo memberInfo)
        {
            return memberInfo.DeclaringType != null && (memberInfo.DeclaringType == ExcludedType || ExcludedType.IsSubclassOf(memberInfo.DeclaringType)) && ExcludedProperties.Any(x => x == memberInfo.Name);
        }
    }
}