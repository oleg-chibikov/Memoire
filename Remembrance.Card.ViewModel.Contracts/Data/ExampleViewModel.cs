using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public sealed class ExampleViewModel : TextEntryViewModel
    {
        [CanBeNull]
        public TextEntryViewModel[] Translations
        {
            get;
            [UsedImplicitly]
            set;
        }
    }
}