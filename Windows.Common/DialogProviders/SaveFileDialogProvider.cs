using Mémoire.Contracts;
using Mémoire.Resources;
using Microsoft.Win32;

namespace Mémoire.Windows.Common.DialogProviders
{
    sealed class SaveFileDialogProvider : ISaveFileDialogProvider
    {
        readonly SaveFileDialog _dialog = new SaveFileDialog
        {
            FileName = DialogProviderConstants.DefaultFilePattern, Filter = DialogProviderConstants.JsonFilesFilter, RestoreDirectory = true, Title = $"{Texts.Title}: {Texts.Export}"
        };

        public string FileName
        {
            get => _dialog.FileName;
            set => _dialog.FileName = value;
        }

        public bool? ShowDialog()
        {
            return _dialog.ShowDialog();
        }
    }
}
