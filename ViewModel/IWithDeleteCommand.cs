using System.Windows.Input;

namespace Remembrance.ViewModel
{
    public interface IWithDeleteCommand
    {
        ICommand DeleteBatchCommand { get; }

        ICommand DeleteCommand { get; }
    }
}
