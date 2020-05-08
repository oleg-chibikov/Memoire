using System.Collections.Generic;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class AvailableLanguagesInfo
    {
        public IReadOnlyDictionary<string, HashSet<string>> Directions { get; set; }

        public IReadOnlyDictionary<string, string> Languages { get; set; }
    }
}
