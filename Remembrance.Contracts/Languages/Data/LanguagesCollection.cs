using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Languages.Data
{
    public sealed class LanguagesCollection : List<Language>
    {
        public LanguagesCollection([NotNull] IEnumerable<Language> languages, [CanBeNull] string? selectedLanguage)
            : base(languages)
        {
            SelectedLanguage = selectedLanguage ?? this.First().Code;
        }

        [NotNull]
        public string SelectedLanguage { get; }
    }
}