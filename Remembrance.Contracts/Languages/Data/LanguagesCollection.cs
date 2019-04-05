using System.Collections.Generic;
using System.Linq;

namespace Remembrance.Contracts.Languages.Data
{
    public sealed class LanguagesCollection : List<Language>
    {
        public LanguagesCollection(IEnumerable<Language> languages, string? selectedLanguage)
            : base(languages)
        {
            SelectedLanguage = selectedLanguage ?? this.First().Code;
        }

        public string SelectedLanguage { get; }
    }
}