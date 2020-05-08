namespace Mémoire.ViewModel
{
    public interface IFocusableViewModel
    {
        bool IsFocused { get; set; }

        bool IsHidden { get; }

        bool IsHiding { get; }
    }
}
