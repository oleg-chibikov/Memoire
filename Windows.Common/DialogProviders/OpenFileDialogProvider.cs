using Mémoire.Contracts;
using Mémoire.Resources;
using Microsoft.Win32;

namespace Mémoire.Windows.Common.DialogProviders
{
    public sealed class OpenFileDialogProvider : IOpenFileDialogProvider
    {
        readonly OpenFileDialog _dialog = new ()
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
