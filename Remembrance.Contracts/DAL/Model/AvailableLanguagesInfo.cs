using System.Collections.Generic;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.DAL.Model
{
    public sealed class AvailableLanguagesInfo
    {
        public IReadOnlyDictionary<string, HashSet<string>> Directions { get; set; }

        public IReadOnlyDictionary<string, string> Languages { get; set; }
    }
}