using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel
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