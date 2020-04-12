using Microsoft.Win32;
using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.Windows.Common.DialogProviders
{
    internal sealed class OpenFileDialogProvider : IOpenFileDialogProvider
    {
        private readonly OpenFileDialog _dialog = new OpenFileDialog
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
