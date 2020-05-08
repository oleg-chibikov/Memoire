using System.Windows.Input;

namespace Mémoire.ViewModel
{
    public interface IWithDeleteCommand
    {
        ICommand DeleteBatchCommand { get; }

        ICommand DeleteCommand { get; }
    }
}
