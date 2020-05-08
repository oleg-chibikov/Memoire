using System.Collections.Generic;
using System.Linq;

namespace Remembrance.Contracts.Languages.Data
{
    public sealed class LanguagesCollection : List<Language>
    {
        public LanguagesCollection(IEnumerable<Language> languages, string? selectedLanguage) : base(languages)
        {
            if ((selectedLanguage == null) || (selectedLanguage == Constants.AutoDetectLanguage))
            {
                SelectedLanguageItem = this[0];
                SelectedLanguage = SelectedLanguageItem.Code;
            }
            else
            {
                // TODO: Better if it would be a dictionary
                SelectedLanguageItem = this.Single(x => x.Code == selectedLanguage);
                SelectedLanguage = selectedLanguage;
            }
        }

        public string SelectedLanguage { get; }

        public Language SelectedLanguageItem { get; }
    }
}
