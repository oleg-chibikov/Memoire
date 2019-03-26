using JetBrains.Annotations;
using Microsoft.Win32;
using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.Windows.Common.DialogProviders
{
    internal sealed class OpenFileDialogProvider : IOpenFileDialogProvider
    {
        [NotNull]
        public string FileName => _dialog.FileName;

        [NotNull]
        private readonly OpenFileDialog _dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            FileName = DialogProviderConstants.DefaultFilePattern,
            Filter = DialogProviderConstants.JsonFilesFilter,
            RestoreDirectory = true,
            Title = $"{Texts.Title}: {Texts.Import}"
        };

        public bool? ShowDialog()
        {
            return _dialog.ShowDialog();
        }
    }
}