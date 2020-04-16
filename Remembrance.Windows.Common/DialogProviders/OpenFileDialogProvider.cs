using Microsoft.Win32;
using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.Windows.Common.DialogProviders
{
    sealed class OpenFileDialogProvider : IOpenFileDialogProvider
    {
        readonly OpenFileDialog _dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            FileName = DialogProviderConstants.DefaultFilePattern,
            Filter = DialogProviderConstants.JsonFilesFilter,
            RestoreDirectory = true,
            Title = $"{Texts.Title}: {Texts.Import}"
        };

        public string FileName => _dialog.FileName;

        public bool? ShowDialog()
        {
            return _dialog.ShowDialog();
        }
    }
}
