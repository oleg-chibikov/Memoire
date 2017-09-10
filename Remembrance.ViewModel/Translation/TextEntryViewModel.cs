using JetBrains.Annotations;
using Remembrance.Contracts;

namespace Remembrance.ViewModel.Translation
{
    public class TextEntryViewModel: IWithText
    {
        [NotNull]
        public virtual string Text { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}