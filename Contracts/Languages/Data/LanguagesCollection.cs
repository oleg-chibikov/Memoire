using System.Collections.Generic;
using System.Linq;
using Scar.Services.Contracts.Data;

namespace Mémoire.Contracts.Languages.Data
{
    public sealed class LanguagesCollection : List<Language>
    {
        public LanguagesCollection(IEnumerable<Language> languages, string? selectedLanguage) : base(languages)
        {
            if ((selectedLanguage == null) || (selectedLanguage == LanguageConstants.AutoDetectLanguage))
            {
                SelectedLanguageItem = this[0];
                SelectedLanguage = SelectedLanguageItem.Code;
            }
            else
            {
                SelectedLanguageItem = this.Single(x => x.Code == selectedLanguage);
                SelectedLanguage = selectedLanguage;
            }
        }

        public string SelectedLanguage { get; }

        public Language SelectedLanguageItem { get; }
    }
}
