using GalaSoft.MvvmLight;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public class TextEntryViewModel : ViewModelBase
    {
        public virtual string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}