using JetBrains.Annotations;
using Microsoft.Win32;
using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.Windows.Common.DialogProviders
{
    internal sealed class SaveFileDialogProvider : ISaveFileDialogProvider
    {
        [NotNull]
        private readonly SaveFileDialog _dialog = new SaveFileDialog
        {
            FileName = DialogProviderConstants.DefaultFilePattern,
            Filter = DialogProviderConstants.JsonFilesFilter,
            RestoreDirectory = true,
            Title = $"{Texts.Title}: {Texts.Export}"
        };

        [NotNull]
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