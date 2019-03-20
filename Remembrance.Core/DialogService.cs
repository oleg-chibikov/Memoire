using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.Core
{
    internal class DialogService : IDialogService
    {
        public bool ConfirmDialog(string message)
        {
            var result = MessageBox.Show(message, Texts.Confirm, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}